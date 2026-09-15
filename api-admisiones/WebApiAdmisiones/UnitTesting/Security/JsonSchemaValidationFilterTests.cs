using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NJsonSchema;
using Xunit;
using WebApiAdmisiones.Security.RequestValidation;

namespace UnitTesting.Security
{
    public class JsonSchemaValidationFilterTests
    {
        private readonly Mock<IJsonSchemaRegistry> _registryMock;
        private readonly Mock<ILogger<JsonSchemaValidationFilter>> _loggerMock;
        private readonly JsonSchemaValidationFilter _filter;

        public JsonSchemaValidationFilterTests()
        {
            _registryMock = new Mock<IJsonSchemaRegistry>();
            _loggerMock = new Mock<ILogger<JsonSchemaValidationFilter>>();
            _filter = new JsonSchemaValidationFilter(_registryMock.Object, _loggerMock.Object);
        }

        private ResourceExecutingContext CreateContext(string method, string path, string? contentType = null, string? accept = null, string? body = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;

            if (contentType != null)
                httpContext.Request.ContentType = contentType;

            if (accept != null)
                httpContext.Request.Headers["Accept"] = accept;

            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                httpContext.Request.Body = new MemoryStream(bytes);
                httpContext.Request.ContentLength = bytes.Length;
            }

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ResourceExecutingContext(actionContext, new List<IFilterMetadata>(), new List<IValueProviderFactory>());
        }

        [Fact]
        public async Task OnResourceExecutionAsync_GetRequest_ContinuesExecution()
        {
            var context = CreateContext("GET", "/api/test");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptDoesNotAllowJson_Adds406()
        {
            var context = CreateContext("POST", "/api/test", accept: "text/html");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(406, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptAllowsJson_Continues()
        {
            var context = CreateContext("POST", "/api/test", accept: "application/json");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptWildcard_Continues()
        {
            var context = CreateContext("POST", "/api/test", accept: "*/*");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptApplicationWildcard_Continues()
        {
            var context = CreateContext("POST", "/api/test", accept: "application/*");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_NoContentType_Adds415()
        {
            var context = CreateContext("POST", "/api/test", body: "{}");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(415, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_InvalidContentType_Adds415()
        {
            var context = CreateContext("POST", "/api/test", contentType: "text/plain", body: "{}");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(415, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_ValidContentType_NoSchema_Continues()
        {
            var context = CreateContext("POST", "/api/test", contentType: "application/json", body: "{}");
            _registryMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<JsonSchema>.IsAny)).Returns(false);
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_InvalidJson_Adds400()
        {
            var context = CreateContext("POST", "/api/test", contentType: "application/json", body: "{invalid}");
            var schema = JsonSchema.FromSampleJson("{}");
            _registryMock.Setup(r => r.TryGet(It.IsAny<string>(), out schema)).Returns(true);
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_ValidJsonInvalidSchema_Adds400()
        {
            var context = CreateContext("POST", "/api/test", contentType: "application/json", body: "{\"name\":\"test\"}");
            var schema = JsonSchema.FromSampleJson("{\"id\":123}");
            schema.RequiredProperties.Add("id");
            _registryMock.Setup(r => r.TryGet(It.IsAny<string>(), out schema)).Returns(true);
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_ValidJsonValidSchema_Continues()
        {
            var context = CreateContext("POST", "/api/test", contentType: "application/json", body: "{\"id\":123}");
            var schema = JsonSchema.FromSampleJson("{\"id\":123}");
            schema.RequiredProperties.Add("id");
            _registryMock.Setup(r => r.TryGet(It.IsAny<string>(), out schema)).Returns(true);
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_PostWithoutBody_Continues()
        {
            var context = CreateContext("POST", "/api/test");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_PutMethod_ValidatesBody()
        {
            var context = CreateContext("PUT", "/api/test", body: "{}");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(415, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_PatchMethod_ValidatesBody()
        {
            var context = CreateContext("PATCH", "/api/test", body: "{}");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(415, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_DeleteMethod_ValidatesBody()
        {
            var context = CreateContext("DELETE", "/api/test", body: "{}");
            Task<ResourceExecutedContext> next() => Task.FromResult<ResourceExecutedContext>(null!);

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(415, result.StatusCode);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_ContentTypeWithCharset_Accepts()
        {
            var context = CreateContext("POST", "/api/test", contentType: "application/json; charset=utf-8", body: "{}");
            _registryMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<JsonSchema>.IsAny)).Returns(false);
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptMultipleValues_OneAllowsJson_Continues()
        {
            var context = CreateContext("POST", "/api/test", accept: "text/html, application/json, text/plain");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_AcceptWithQuality_AllowsJson()
        {
            var context = CreateContext("POST", "/api/test", accept: "application/json;q=0.9");
            var nextCalled = false;
            Task<ResourceExecutedContext> next() { nextCalled = true; return Task.FromResult<ResourceExecutedContext>(null!); }

            await _filter.OnResourceExecutionAsync(context, next);

            Assert.True(nextCalled);
        }

        [Fact]
        public void InMemoryJsonSchemaRegistry_BuildKey_ReturnsCorrectFormat()
        {
            var key = InMemoryJsonSchemaRegistry.BuildKey("POST", "/api/test");
            Assert.Equal("POST /api/test", key);
        }

        [Fact]
        public void InMemoryJsonSchemaRegistry_BuildKey_NormalizesCase()
        {
            var key1 = InMemoryJsonSchemaRegistry.BuildKey("post", "/API/TEST");
            var key2 = InMemoryJsonSchemaRegistry.BuildKey("POST", "/api/test");
            Assert.Equal(key1.ToUpperInvariant(), key2.ToUpperInvariant());
        }

        [Fact]
        public void InMemoryJsonSchemaRegistry_TryGet_ReturnsSchema()
        {
            var registry = new InMemoryJsonSchemaRegistry();
            var found = registry.TryGet("POST /api/datospersonales", out var schema);
            Assert.True(found);
            Assert.NotNull(schema);
        }

        [Fact]
        public void InMemoryJsonSchemaRegistry_TryGet_NotFound_ReturnsFalse()
        {
            var registry = new InMemoryJsonSchemaRegistry();
            var found = registry.TryGet("GET /api/nonexistent", out var schema);
            Assert.False(found);
        }

        [Fact]
        public void JsonSchemaRegistry_BuildKey_WithPathString_ReturnsCorrectFormat()
        {
            var path = new PathString("/api/test");
            var key = JsonSchemaRegistry.BuildKey("POST", path);
            Assert.Equal("POST /api/test", key);
        }
    }
}

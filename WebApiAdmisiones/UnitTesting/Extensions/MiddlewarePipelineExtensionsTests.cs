using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApiAdmisiones.Extensions;
using Xunit;

namespace UnitTesting.Extensions
{
    public class MiddlewarePipelineExtensionsTests
    {
        [Fact]
        public void ConfigureMiddlewarePipeline_WithProductionEnvironment_DoesNotThrow()
        {
            var builder = CreateWebApplicationBuilder("Production");
            var app = builder.Build();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "JWT:RefreshThresholdMinutes", "10" } })
                .Build();

            var exception = Record.Exception(() => app.ConfigureMiddlewarePipeline(configuration));

            Assert.Null(exception);
        }

        [Fact]
        public void ConfigureMiddlewarePipeline_WithDevelopmentEnvironment_ExecutesSwaggerBranch()
        {
            var builder = CreateWebApplicationBuilder("Development");
            var app = builder.Build();
            var configuration = new ConfigurationBuilder().Build();

            var exception = Record.Exception(() => app.ConfigureMiddlewarePipeline(configuration));

            Assert.Null(exception);
        }

        [Fact]
        public async Task UseSecurityHeaders_AddsExpectedHeadersAndInvokesNext()
        {
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            var method = GetPrivateMiddlewareMethod("UseSecurityHeaders");
            var configuredBuilder = (IApplicationBuilder)method.Invoke(null, new object[] { builder })!;
            var nextCalled = false;
            configuredBuilder.Run(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            var pipeline = configuredBuilder.Build();
            var context = CreateHttpContext();

            await pipeline(context);

            Assert.True(nextCalled);
            Assert.Contains("max-age=31536000", context.Response.Headers.StrictTransportSecurity.ToString());
            Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions.ToString());
            Assert.Equal("DENY", context.Response.Headers.XFrameOptions.ToString());
            Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
            Assert.Equal("0", context.Response.Headers.XXSSProtection.ToString());
            Assert.Contains("default-src 'self'", context.Response.Headers.ContentSecurityPolicy.ToString());
            Assert.Equal("geolocation=(), microphone=(), camera=()", context.Response.Headers["Permissions-Policy"].ToString());
        }

        [Fact]
        public async Task UseOptionsPreflight_OptionsRequestShortCircuitsAndSkipsNext()
        {
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            var method = GetPrivateMiddlewareMethod("UseOptionsPreflight");
            var configuredBuilder = (IApplicationBuilder)method.Invoke(null, new object[] { builder })!;
            var nextCalled = false;
            configuredBuilder.Run(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            var pipeline = configuredBuilder.Build();
            var context = CreateHttpContext();
            context.Request.Method = HttpMethods.Options;

            await pipeline(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task UseOptionsPreflight_NonOptionsRequestInvokesNext()
        {
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            var method = GetPrivateMiddlewareMethod("UseOptionsPreflight");
            var configuredBuilder = (IApplicationBuilder)method.Invoke(null, new object[] { builder })!;
            var nextCalled = false;
            configuredBuilder.Run(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            var pipeline = configuredBuilder.Build();
            var context = CreateHttpContext();
            context.Request.Method = HttpMethods.Get;

            await pipeline(context);

            Assert.True(nextCalled);
            Assert.Equal(HttpMethods.Get, context.Request.Method);
        }

        private static WebApplicationBuilder CreateWebApplicationBuilder(string environmentName)
        {
            var options = new WebApplicationOptions
            {
                EnvironmentName = environmentName,
                ApplicationName = typeof(MiddlewarePipelineExtensionsTests).Assembly.FullName,
                ContentRootPath = Directory.GetCurrentDirectory()
            };

            var builder = WebApplication.CreateBuilder(options);
            builder.Services.AddLogging();
            builder.Services.AddCors(o => o.AddPolicy("AllowAngularApp", policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }));
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(_ => { });
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            return builder;
        }

        private static MethodInfo GetPrivateMiddlewareMethod(string name)
        {
            return typeof(MiddlewarePipelineExtensions).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException($"Could not find private middleware '{name}'.");
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext();
        }
    }
}

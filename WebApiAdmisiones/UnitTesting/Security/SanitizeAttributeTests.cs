using System;
using System.Collections.Generic;
using System.Reflection;
using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Sanitization.Code;
using Xunit;

namespace UnitTesting.Security
{
    public class SanitizeAttributeTests
    {
        private ActionExecutingContext CreateContext(object argument)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            var actionDescriptor = new ControllerActionDescriptor();
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, new ModelStateDictionary());

            var actionArguments = new Dictionary<string, object?> { { "model", argument } };
            var filters = new List<IFilterMetadata>();
            var controller = new object();

            return new ActionExecutingContext(actionContext, filters, actionArguments, controller);
        }

        public class TestModel
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public NestedModel? Nested { get; set; }

            [AllowHtml]
            public string? HtmlContent { get; set; }
        }

        public class NestedModel
        {
            public string? Comment { get; set; }
        }

        [Fact]
        public void OnActionExecuting_SanitizesStringProperties()
        {
            var model = new TestModel
            {
                Name = "<script>alert('x')</script>",
                Description = "<b>desc</b>",
                HtmlContent = "<div>safe</div>"
            };
            var context = CreateContext(model);
            var attr = new SanitizeAttribute();

            attr.OnActionExecuting(context);

            var sanitized = (TestModel)context.ActionArguments["model"]!;
            Assert.DoesNotContain("<script>", sanitized.Name);
            Assert.Contains("<b>", sanitized.Description); // <b> is allowed by default
        }

        [Fact]
        public void OnActionExecuting_DoesNotSanitize_AllowHtmlProperties()
        {
            var model = new TestModel
            {
                HtmlContent = "<script>alert('x')</script>"
            };
            var context = CreateContext(model);
            var attr = new SanitizeAttribute();

            attr.OnActionExecuting(context);

            var sanitized = (TestModel)context.ActionArguments["model"]!;
            Assert.Equal("<script>alert('x')</script>", sanitized.HtmlContent);
        }

        [Fact]
        public void OnActionExecuting_SanitizesNestedObjects()
        {
            var model = new TestModel
            {
                Nested = new NestedModel { Comment = "<img src=x onerror=alert(1)>" }
            };
            var context = CreateContext(model);
            var attr = new SanitizeAttribute();

            attr.OnActionExecuting(context);

            var sanitized = (TestModel)context.ActionArguments["model"]!;
            Assert.DoesNotContain("onerror", sanitized.Nested!.Comment);
        }

        [Fact]
        public void OnActionExecuting_SanitizesStringArgument()
        {
            var context = CreateContext("<script>alert('x')</script>");
            var attr = new SanitizeAttribute();

            attr.OnActionExecuting(context);

            var sanitized = (string)context.ActionArguments["model"]!;
            Assert.DoesNotContain("<script>", sanitized);
        }

        [Fact]
        public void SanitizeObject_ThrowsInputSanitizationException_OnSetterError()
        {
            var model = new ModelWithThrowingSetter();
            model._name = "<b>bad</b>"; // 👈 evitar que se dispare el setter

            var attr = new SanitizeAttribute();
            var context = CreateContext(model);

            var ex = Assert.Throws<InputSanitizationException>(() =>
                attr.OnActionExecuting(context)
            );

            Assert.Contains("Error sanitizing property", ex.Message);
        }

        public class ModelWithThrowingSetter
        {
            public string? _name;
            public string? Name
            {
                get => _name;
                set => throw new InvalidOperationException("Setter error");
            }
        }
    }
}

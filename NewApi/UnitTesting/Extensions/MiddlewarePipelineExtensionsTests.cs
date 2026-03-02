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
using WebApiFDP.Extensions;
using WebApiFDP.Security;
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

        [Fact]
        public async Task UseJwtTokenRefresh_WhenIssuerMissing_DoesNotAddToken()
        {
            var user = CreateAuthenticatedUser(expirationMinutes: 5, includeIssuer: false, includeAudience: true);
            var pipeline = BuildJwtRefreshPipeline(new Dictionary<string, string?>());
            var context = CreateHttpContext();
            context.User = user;

            await pipeline(context);
            await ExecuteOnStartingCallbacks(context.Response);

            Assert.False(context.Response.Headers.ContainsKey("x-token"));
        }

        [Fact]
        public async Task UseJwtTokenRefresh_WhenUserNotAuthenticated_DoesNothing()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var pipeline = BuildJwtRefreshPipeline(new Dictionary<string, string?>());
            var context = CreateHttpContext();
            context.User = user;

            await pipeline(context);
            await ExecuteOnStartingCallbacks(context.Response);

            Assert.False(context.Response.Headers.ContainsKey("x-token"));
        }

        [Fact]
        public async Task UseJwtTokenRefresh_WhenTokenNotNearExpiration_SkipsRefresh()
        {
            var user = CreateAuthenticatedUser(expirationMinutes: 120, includeIssuer: true, includeAudience: true);
            var pipeline = BuildJwtRefreshPipeline(new Dictionary<string, string?> { { "JWT:RefreshThresholdMinutes", "15" } });
            var context = CreateHttpContext();
            context.User = user;

            await pipeline(context);
            await ExecuteOnStartingCallbacks(context.Response);

            Assert.False(context.Response.Headers.ContainsKey("x-token"));
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

        private static ClaimsPrincipal CreateAuthenticatedUser(int expirationMinutes, bool includeIssuer, bool includeAudience)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "unit-test-user"),
                new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds().ToString())
            };

            if (includeIssuer)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Iss, "https://issuer"));
            }

            if (includeAudience)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "https://audience"));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
            return new ClaimsPrincipal(identity);
        }

        private static RequestDelegate BuildJwtRefreshPipeline(IDictionary<string, string?> configurationValues)
        {
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurationValues).Build();
            var method = GetPrivateMiddlewareMethod("UseJwtTokenRefresh");
            var configuredBuilder = (IApplicationBuilder)method.Invoke(null, new object[] { builder, configuration })!;
            configuredBuilder.Run(async ctx =>
            {
                await ctx.Response.WriteAsync("ok");
            });

            return configuredBuilder.Build();
        }

        private static Task ExecuteOnStartingCallbacks(HttpResponse response)
        {
            // Ensures ASP.NET Core executes registered OnStarting callbacks even when the
            // response body never flushes to the actual server transport during tests.
            return response.StartAsync();
        }


    }
}

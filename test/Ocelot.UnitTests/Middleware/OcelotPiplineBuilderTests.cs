using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Errors.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Reflection;

namespace Ocelot.UnitTests.Middleware
{
    public class OcelotPiplineBuilderTests : UnitTest
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configRoot;
        private int _counter;
        private readonly HttpContext _httpContext;

        public OcelotPiplineBuilderTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton(GetHostingEnvironment());
            _services.AddSingleton(_configRoot);
            _services.AddOcelot();
            _httpContext = new DefaultHttpContext();
        }

        private static IWebHostEnvironment GetHostingEnvironment()
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment
                .Setup(e => e.ApplicationName)
                .Returns(typeof(OcelotPiplineBuilderTests).GetTypeInfo().Assembly.GetName().Name);

            return environment.Object;
        }

        [Fact]
        public void should_build_generic()
        {
            this.When(x => WhenIUseAGeneric())
                .Then(x => ThenTheGenericIsInThePipeline())
                .BDDfy();
        }

        [Fact]
        public void should_build_func()
        {
            this.When(x => WhenIUseAFunc())
                .Then(x => ThenTheFuncIsInThePipeline())
                .BDDfy();
        }

        private void WhenIUseAGeneric()
        {
            var provider = _services.BuildServiceProvider();
            IApplicationBuilder builder = new ApplicationBuilder(provider);
            builder = builder.UseMiddleware<ExceptionHandlerMiddleware>();
            var del = builder.Build();
            del.Invoke(_httpContext);
        }

        private void ThenTheGenericIsInThePipeline()
        {
            _httpContext.Response.StatusCode.ShouldBe(500);
        }

        private void WhenIUseAFunc()
        {
            _counter = 0;
            var provider = _services.BuildServiceProvider();
            IApplicationBuilder builder = new ApplicationBuilder(provider);
            builder = builder.Use(async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            });
            var del = builder.Build();
            del.Invoke(_httpContext);
        }

        private void ThenTheFuncIsInThePipeline()
        {
            _counter.ShouldBe(1);
            _httpContext.Response.StatusCode.ShouldBe(404);
        }

        [Fact]
        public void Middleware_Multi_Parameters_Invoke()
        {
            var provider = _services.BuildServiceProvider();
            IApplicationBuilder builder = new ApplicationBuilder(provider);
            builder = builder.UseMiddleware<MultiParametersInvokeMiddleware>();
            var del = builder.Build();
            del.Invoke(_httpContext);
        }

        private class MultiParametersInvokeMiddleware : OcelotMiddleware
        {
            private readonly RequestDelegate _next;

            public MultiParametersInvokeMiddleware(RequestDelegate next)
                : base(new FakeLogger())
            {
                _next = next;
            }

            public Task Invoke(HttpContext context, IServiceProvider serviceProvider)
            {
                return Task.CompletedTask;
            }
        }
    }

    internal class FakeLogger : IOcelotLogger
    {
        public void LogCritical(string message, Exception exception) { }

        public void LogCritical(Func<string> messageFactory, Exception exception) { }

        public void LogError(string message, Exception exception) { }

        public void LogError(Func<string> messageFactory, Exception exception) { }

        public void LogDebug(string message) { }

        public void LogDebug(Func<string> messageFactory) { }

        public void LogInformation(string message) { }

        public void LogInformation(Func<string> messageFactory) { }

        public void LogWarning(string message) { }

        public void LogTrace(string message) { }

        public void LogTrace(Func<string> messageFactory) { }

        public void LogWarning(Func<string> messageFactory) { }
    }
}

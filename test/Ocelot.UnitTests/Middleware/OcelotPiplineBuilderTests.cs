using System;
using System.Threading.Tasks;

namespace Ocelot.UnitTests.Middleware
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Pipeline;
    using Shouldly;
    using System.Collections.Generic;
    using TestStack.BDDfy;
    using Xunit;

    public class OcelotPiplineBuilderTests
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configRoot;
        private DownstreamContext _downstreamContext;
        private int _counter;

        public OcelotPiplineBuilderTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
            _services.AddSingleton<IConfiguration>(_configRoot);
            _services.AddOcelot();
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
            IOcelotPipelineBuilder builder = new OcelotPipelineBuilder(provider);
            builder = builder.UseMiddleware<Ocelot.Errors.Middleware.ExceptionHandlerMiddleware>();
            var del = builder.Build();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            del.Invoke(_downstreamContext);
        }

        private void ThenTheGenericIsInThePipeline()
        {
            _downstreamContext.HttpContext.Response.StatusCode.ShouldBe(500);
        }

        private void WhenIUseAFunc()
        {
            _counter = 0;
            var provider = _services.BuildServiceProvider();
            IOcelotPipelineBuilder builder = new OcelotPipelineBuilder(provider);
            builder = builder.Use(async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            });
            var del = builder.Build();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            del.Invoke(_downstreamContext);
        }

        private void ThenTheFuncIsInThePipeline()
        {
            _counter.ShouldBe(1);
            _downstreamContext.HttpContext.Response.StatusCode.ShouldBe(404);
        }

        [Fact]
        public void Middleware_Multi_Parameters_Invoke()
        {
            var provider = _services.BuildServiceProvider();
            IOcelotPipelineBuilder builder = new OcelotPipelineBuilder(provider);
            builder = builder.UseMiddleware<MultiParametersInvokeMiddleware>();
            var del = builder.Build();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            del.Invoke(_downstreamContext);
        }

        private class MultiParametersInvokeMiddleware : OcelotMiddleware
        {
            private readonly OcelotRequestDelegate _next;

            public MultiParametersInvokeMiddleware(OcelotRequestDelegate next)
                : base(new FakeLogger())
            {
                _next = next;
            }

            public Task Invoke(DownstreamContext context, IServiceProvider serviceProvider)
            {
                return Task.CompletedTask;
            }
        }
    }

    internal class FakeLogger : IOcelotLogger
    {
        public void LogCritical(string message, Exception exception)
        {
        }

        public void LogDebug(string message)
        {
        }

        public void LogError(string message, Exception exception)
        {
        }

        public void LogInformation(string message)
        {
        }

        public void LogTrace(string message)
        {
        }

        public void LogWarning(string message)
        {
        }
    }
}

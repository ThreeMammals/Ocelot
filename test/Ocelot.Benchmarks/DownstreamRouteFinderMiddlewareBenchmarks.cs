using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Benchmarks
{
    [SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 5)]
    [Config(typeof(DownstreamRouteFinderMiddlewareBenchmarks))]
    public class DownstreamRouteFinderMiddlewareBenchmarks : ManualConfig
    {
        private DownstreamRouteFinderMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public DownstreamRouteFinderMiddlewareBenchmarks()
        {
            Add(StatisticColumn.AllStatistics);
            Add(MemoryDiagnoser.Default);
            Add(BaselineValidator.FailOnError);
        }

        [GlobalSetup]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection();
            var config = new ConfigurationRoot(new List<IConfigurationProvider>());
            var builder = new OcelotBuilder(serviceCollection, config);
            var services = serviceCollection.BuildServiceProvider();
            var loggerFactory = services.GetService<IOcelotLoggerFactory>();
            var drpf = services.GetService<IDownstreamRouteProviderFactory>();
            var multiplexer = services.GetService<IMultiplexer>();

            _next = async context =>
            {
                await Task.CompletedTask;
                throw new Exception("BOOM");
            };

            _middleware = new DownstreamRouteFinderMiddleware(_next, loggerFactory, drpf, multiplexer);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString("/test");
            httpContext.Request.QueryString = new QueryString("?a=b");
            httpContext.Request.Headers.Add("Host", "most");

            _downstreamContext = new DownstreamContext(httpContext)
            {
                Configuration = new InternalConfiguration(new List<ReRoute>(), null, null, null, null, null, null, null)
            };
        }

        [Benchmark(Baseline = true)]
        public async Task Baseline()
        {
            await _middleware.Invoke(_downstreamContext);
        }
    }
}

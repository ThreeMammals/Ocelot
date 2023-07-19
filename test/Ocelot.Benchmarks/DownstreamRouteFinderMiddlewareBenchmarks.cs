using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Validators;

using Ocelot.Configuration;

using Ocelot.DependencyInjection;

using Ocelot.DownstreamRouteFinder.Finder;

using Ocelot.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.Middleware;

using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.Benchmarks
{
    [SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 5)]
    [Config(typeof(DownstreamRouteFinderMiddlewareBenchmarks))]
    public class DownstreamRouteFinderMiddlewareBenchmarks : ManualConfig
    {
        private DownstreamRouteFinderMiddleware _middleware;
        private RequestDelegate _next;
        private HttpContext _httpContext;

        public DownstreamRouteFinderMiddlewareBenchmarks()
        {
            AddColumn(StatisticColumn.AllStatistics);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddValidator(BaselineValidator.FailOnError);
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

            _next = async context =>
            {
                await Task.CompletedTask;
                throw new Exception("BOOM");
            };

            _middleware = new DownstreamRouteFinderMiddleware(_next, loggerFactory, drpf);

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Path = new PathString("/test"),
                    QueryString = new QueryString("?a=b"),
                },
            };
            httpContext.Request.Headers.Add("Host", "most");
            httpContext.Items.SetIInternalConfiguration(new InternalConfiguration(new List<Route>(), null, null, null, null, null, null, null, null));

            _httpContext = httpContext;
        }

        [Benchmark(Baseline = true)]
        public async Task Baseline()
        {
            await _middleware.Invoke(_httpContext);
        }
    }
}

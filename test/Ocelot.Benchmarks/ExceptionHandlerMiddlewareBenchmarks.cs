using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Errors.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.Benchmarks
{
    [SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5)]
    [Config(typeof(ExceptionHandlerMiddlewareBenchmarks))]
    public class ExceptionHandlerMiddlewareBenchmarks : ManualConfig
    {
        private ExceptionHandlerMiddleware _middleware;
        private RequestDelegate _next;
        private HttpContext _httpContext;

        public ExceptionHandlerMiddlewareBenchmarks()
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
            var repo = services.GetService<IRequestScopedDataRepository>();

            _next = async context =>
            {
                await Task.CompletedTask;
                throw new Exception("BOOM");
            };

            _middleware = new ExceptionHandlerMiddleware(_next, loggerFactory, repo);
            _httpContext = new DefaultHttpContext();
        }

        [Benchmark(Baseline = true)]
        public async Task Baseline()
        {
            await _middleware.Invoke(_httpContext);
        }
    }
}

using BenchmarkDotNet.Running;

namespace Ocelot.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher(
            new[]
            {
                typeof(UrlPathToUrlPathTemplateMatcherBenchmarks),
                typeof(AllTheThingsBenchmarks),
                typeof(ExceptionHandlerMiddlewareBenchmarks),
                typeof(DownstreamRouteFinderMiddlewareBenchmarks),
                typeof(SerilogBenchmarks),
                typeof(MsLoggerBenchmarks),
                typeof(PayloadBenchmarks),
                typeof(ResponseBenchmarks),
            });
        switcher.Run(args);
    }
}

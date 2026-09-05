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
                typeof(JsonSerializerBenchmark),
            });

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddAnalyser(BenchmarkDotNet.Analysers.EnvironmentAnalyser.Default)
            .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
            .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.OperationsPerSecond);

        switcher.Run(args, config);
    }
}

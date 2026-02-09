using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ocelot.Benchmarks;

[Config(typeof(AllTheThingsBenchmarks))]
public sealed class AllTheThingsBenchmarks : ManualConfig, IDisposable
{
    private readonly BenchmarkSteps steps = new();
    public void Dispose() => steps.Dispose();

    public AllTheThingsBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    [GlobalSetup]
    public void SetUp()
    {
        var port = PortFinder.GetRandomPort();
        var route = steps.GivenDefaultRoute(port);
        var configuration = steps.GivenConfiguration(route);
        steps.GivenThereIsAServiceRunningOn(port, "/", 201, string.Empty);
        steps.GivenThereIsAConfiguration(configuration);
        steps.GivenOcelotIsRunning(
            host => host.ConfigureLogging(
                (c, l) => l.AddConfiguration(c.Configuration.GetSection("Logging"))) // Action<IWebHostBuilder> postConfigureHost,
        );
    }

    [Benchmark(Baseline = true)]
    public Task Baseline()
    {
        return steps.WhenIGetUrlOnTheApiGateway("/");
    }

    /* Summary
            BenchmarkDotNet = v0.10.13, OS = macOS 10.12.6 (16G1212) [Darwin 16.7.0]
            Intel Core i5-4278U CPU 2.60GHz(Haswell), 1 CPU, 4 logical cores and 2 physical cores
           .NET Core SDK = 2.1.4

             [Host]     : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT
             DefaultJob : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT
                Method |     Mean |     Error |    StdDev |    StdErr |      Min |       Q1 |   Median |       Q3 |      Max |  Op/s | Scaled |   Gen 0 |  Gen 1 | Allocated |
             --------- |---------:|----------:|----------:|----------:|---------:|---------:|---------:|---------:|---------:|------:|-------:|--------:|-------:|----------:|
              Baseline | 2.102 ms | 0.0292 ms | 0.0273 ms | 0.0070 ms | 2.063 ms | 2.080 ms | 2.093 ms | 2.122 ms | 2.152 ms | 475.8 |   1.00 | 31.2500 | 3.9063 |   1.63 KB |
    */
}

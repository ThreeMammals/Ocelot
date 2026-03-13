using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net;

namespace Ocelot.Benchmarks;

[Config(typeof(MsLoggerBenchmarks))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MaxIterationCount(16)]
public sealed class MsLoggerBenchmarks : ManualConfig, IDisposable
{
    private readonly BenchmarkSteps steps = new();
    public void Dispose() => steps.Dispose();

    public MsLoggerBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    private Task SendRequest()
    {
        return steps.WhenIGetUrlOnTheApiGateway("/");
    }

    [Benchmark(Baseline = true)]
    public async Task LogLevelCritical() => await SendRequest();

    [GlobalSetup(Target = nameof(LogLevelCritical))]
    public void SetUpCritical() => OcelotFactory(LogLevel.Critical);

    [Benchmark]
    public async Task LogLevelError() => await SendRequest();

    [GlobalSetup(Target = nameof(LogLevelError))]
    public void SetupError() => OcelotFactory(LogLevel.Error);

    [Benchmark]
    public async Task LogLevelWarning() => await SendRequest();

    [GlobalSetup(Target = nameof(LogLevelWarning))]
    public void SetUpWarning() => OcelotFactory(LogLevel.Warning);

    [Benchmark]
    public async Task LogLevelInformation() => await SendRequest();

    [GlobalSetup(Target = nameof(LogLevelInformation))]
    public void SetUpInformation() => OcelotFactory(LogLevel.Information);

    [Benchmark]
    public async Task LogLevelTrace() => await SendRequest();

    [GlobalSetup(Target = nameof(LogLevelTrace))]
    public void SetUpTrace() => OcelotFactory(LogLevel.Trace);

    [GlobalCleanup(Targets = new[]
    {
        nameof(LogLevelCritical), nameof(LogLevelError), nameof(LogLevelWarning), nameof(LogLevelInformation),
        nameof(LogLevelTrace),
    })]
    public void OcelotCleanup()
    {
        steps.Dispose();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    private void GivenOcelotIsRunning(LogLevel minLogLevel)
        => steps.GivenOcelotIsRunning(
            async app =>
            {
                app.Use(async (context, next) =>
                {
                    var loggerFactory = context.RequestServices.GetService<IOcelotLoggerFactory>();
                    var ocelotLogger = loggerFactory.CreateLogger<MsLoggerBenchmarks>();
                    ocelotLogger.LogDebug(() => $"DEBUG: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogTrace(() => $"TRACE: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogInformation(() => $"INFORMATION: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogWarning(() => $"WARNING: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogError(() => $"ERROR: {nameof(ocelotLogger)},  {nameof(loggerFactory)}",
                        new Exception("test"));
                    ocelotLogger.LogCritical(() => $"CRITICAL: {nameof(ocelotLogger)},  {nameof(loggerFactory)}",
                        new Exception("test"));

                    await next.Invoke();
                });
                await app.UseOcelot();
            }, // Action<IApplicationBuilder> configureApp
            host => host.ConfigureLogging(
                (c, l) => l.ClearProviders().SetMinimumLevel(minLogLevel).AddConsole()) // Action<IWebHostBuilder> postConfigureHost
        );

    private void OcelotFactory(LogLevel minLogLevel)
    {
        var port = PortFinder.GetRandomPort();
        var route = steps.GivenDefaultRoute(port);
        var configuration = steps.GivenConfiguration(route);
        steps.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, nameof(MsLoggerBenchmarks));
        steps.GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(minLogLevel);
    }
}

using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Benchmarks;

[Config(typeof(MsLoggerBenchmarks))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MaxIterationCount(16)]
public class MsLoggerBenchmarks : ManualConfig
{
    private IWebHost _service;
    private IWebHost _webHost;
    private HttpClient _httpClient;

    public MsLoggerBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    private async Task SendRequest()
    {
        _httpClient ??= new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
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
        _webHost?.Dispose();
        _service?.Dispose();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    private void GivenOcelotIsRunning(string url, LogLevel minLogLevel)
    {
        _webHost = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(url)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("ocelot.json", false, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices(s => { s.AddOcelot(); })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(minLogLevel);
                logging.AddConsole();
            })
            .Configure(async app =>
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
            })
            .Build();

        _webHost.Start();
    }

    private void OcelotFactory(LogLevel minLogLevel)
    {
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = 51879,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                },
            },
        };

        GivenThereIsAConfiguration(configuration);
        GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 201, string.Empty);
        GivenOcelotIsRunning("http://localhost:5000", minLogLevel);
    }

    public static void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
    {
        var configurationPath = Path.Combine(AppContext.BaseDirectory, "ocelot.json");

        var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

        if (File.Exists(configurationPath))
        {
            File.Delete(configurationPath);
        }

        File.WriteAllText(configurationPath, jsonConfiguration);
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
    {
        _service = new WebHostBuilder()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(async context =>
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                });
            })
            .Build();

        _service.Start();
    }
}

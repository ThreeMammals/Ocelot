using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using Serilog;
using Serilog.Core;

namespace Ocelot.AcceptanceTests;

public sealed class LogLevelTests : Steps
{
    private readonly string _logFileName;
    private readonly string _appSettingsFileName;

    private const string AppSettingsFormat =
        "{{\"Logging\":{{\"LogLevel\":{{\"Default\":\"{0}\",\"System\":\"{0}\",\"Microsoft\":\"{0}\"}}}}}}";

    public LogLevelTests()
    {
        _logFileName = $"ocelot_logs_{TestID}.log";
        _appSettingsFileName = $"appsettings_{TestID}.json";
        Files.Add(_logFileName);
        Files.Add(_appSettingsFileName);
    }

    private void ThenMessagesAreLogged(string[] notAllowedMessageTypes, string[] allowedMessageTypes)
    {
        var logFilePath = GetLogFilePath();
        var logFileContent = File.ReadAllText(logFilePath);
        var logFileLines = logFileContent.Split(Environment.NewLine);

        var logFileLinesWithLogLevel = logFileLines.Where(x => notAllowedMessageTypes.Any(x.Contains)).ToList();
        logFileLinesWithLogLevel.Count.ShouldBe(0);

        var logFileLinesWithAllowedLogLevel = logFileLines.Where(x => allowedMessageTypes.Any(x.Contains)).ToList();
        logFileLinesWithAllowedLogLevel.Count.ShouldBe(2 * allowedMessageTypes.Length);
    }

    private void TestFactory(string[] notAllowedMessageTypes, string[] allowedMessageTypes, LogLevel level)
    {
        var port = PortFinder.GetRandomPort();
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
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Get"],
                    RequestIdKey = "Oc-RequestId",
                },
            },
        };

        using var logger = GetLogger(level);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithMinimumLogLevel(logger, _appSettingsFileName))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .Then(x => logger.Dispose())
            .Then(x => ThenMessagesAreLogged(notAllowedMessageTypes, allowedMessageTypes))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithMinimumLogLevel(Logger logger, string appsettingsFileName)
    {
        var builder = TestHostBuilder.Create()
            .UseKestrel()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile(appsettingsFileName, false, false);
                config.AddJsonFile(ocelotConfigFileName, false, false);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(s => { s.AddOcelot(); })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog(logger);
            })
            .Configure(async app =>
            {
                app.Use(async (context, next) =>
                {
                    var loggerFactory = context.RequestServices.GetService<IOcelotLoggerFactory>();
                    var ocelotLogger = loggerFactory.CreateLogger<Steps>();
                    ocelotLogger.LogDebug(() => $"DEBUG: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogTrace(() => $"TRACE: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogInformation(() =>
                        $"INFORMATION: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogWarning(() => $"WARNING: {nameof(ocelotLogger)},  {nameof(loggerFactory)}");
                    ocelotLogger.LogError(() => $"ERROR: {nameof(ocelotLogger)},  {nameof(loggerFactory)}",
                        new Exception("test"));
                    ocelotLogger.LogCritical(() => $"CRITICAL: {nameof(ocelotLogger)},  {nameof(loggerFactory)}",
                        new Exception("test"));

                    await next.Invoke();
                });
                await app.UseOcelot();
            });

        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }

    [Fact]
    public void If_minimum_log_level_is_critical_then_only_critical_messages_are_logged() => TestFactory(
        [ "TRACE", "INFORMATION", "WARNING", "ERROR" ],
        [ "CRITICAL" ], LogLevel.Critical);

    [Fact]
    public void If_minimum_log_level_is_error_then_critical_and_error_are_logged() => TestFactory(
        [ "TRACE", "INFORMATION", "WARNING", "DEBUG" ],
        [ "CRITICAL", "ERROR" ], LogLevel.Error);

    [Fact]
    public void If_minimum_log_level_is_warning_then_critical_error_and_warning_are_logged() => TestFactory(
        [ "TRACE", "INFORMATION", "DEBUG" ],
        [ "CRITICAL", "ERROR", "WARNING" ], LogLevel.Warning);
    
    [Fact]
    public void If_minimum_log_level_is_information_then_critical_error_warning_and_information_are_logged() => TestFactory(
        [ "TRACE", "DEBUG" ],
        [ "CRITICAL", "ERROR", "WARNING", "INFORMATION" ], LogLevel.Information);

    [Fact]
    public void If_minimum_log_level_is_debug_then_critical_error_warning_information_and_debug_are_logged() => TestFactory(
        [ "TRACE" ],
        [ "DEBUG", "CRITICAL", "ERROR", "WARNING", "INFORMATION" ], LogLevel.Debug);

    [Fact]  
    public void If_minimum_log_level_is_trace_then_critical_error_warning_information_debug_and_trace_are_logged() => TestFactory(
        [],
        [ "TRACE", "DEBUG", "CRITICAL", "ERROR", "WARNING", "INFORMATION" ], LogLevel.Trace);

    private Logger GetLogger(LogLevel logLevel)
    {
        var logFilePath = GetLogFilePath();
        UpdateAppSettings(logLevel);
        var logger = logLevel switch
        {
            LogLevel.Information => new LoggerConfiguration().MinimumLevel.Information()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.Warning => new LoggerConfiguration().MinimumLevel.Warning()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.Error => new LoggerConfiguration().MinimumLevel.Error()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.Critical => new LoggerConfiguration().MinimumLevel.Fatal()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.Debug => new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.Trace => new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            LogLevel.None => new LoggerConfiguration()
                .WriteTo.File(logFilePath)
                .CreateLogger(),
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
        };
        return logger;
    }

    private void UpdateAppSettings(LogLevel logLevel)
    {
        var appSettingsFilePath = Path.Combine(AppContext.BaseDirectory, _appSettingsFileName);
        if (File.Exists(appSettingsFilePath))
        {
            File.Delete(appSettingsFilePath);
        }

        var appSettings = string.Format(AppSettingsFormat, Enum.GetName(typeof(LogLevel), logLevel));
        File.WriteAllText(appSettingsFilePath, appSettings);
    }

    private string GetLogFilePath()
    {
        var logFilePath = Path.Combine(AppContext.BaseDirectory, _logFileName);
        return logFilePath;
    }

    private void GivenThereIsAServiceRunningOn(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Response.StatusCode = 200;
            return context.Response.WriteAsync(string.Empty);
        });
    }
}

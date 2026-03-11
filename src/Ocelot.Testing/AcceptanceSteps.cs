using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Shouldly;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CookieHeaderValue = Microsoft.Net.Http.Headers.CookieHeaderValue;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Ocelot.Testing;

/// <summary>
/// This is the base class for acceptance testing classes, specifically for developing Step classes.
/// It is the recommended base class to inherit from.
/// </summary>
public class AcceptanceSteps : IDisposable
{
    protected IHost? ocelotHost;
    protected TestServer? ocelotServer;
    protected HttpClient? ocelotClient;
    protected HttpResponseMessage? response;

    private readonly Guid _testId;
    protected readonly Random random;
    protected readonly string ocelotConfigFileName;
    protected readonly ServiceHandler handler;

    public AcceptanceSteps()
    {
        _testId = Guid.NewGuid();
        random = new Random();
        ocelotConfigFileName = $"ocelot-{_testId:N}.json"; // {ConfigurationBuilderExtensions.PrimaryConfigFile}";
        Files = [ocelotConfigFileName];
        Folders = [];
        handler = new();
    }

    protected List<string> Files { get; }
    protected List<string> Folders { get; }
    protected virtual string TestID { get => _testId.ToString("N"); }
    public virtual string TestName([CallerMemberName] string? testName = null) => testName ?? GetType().Name; // but it could be TestID also

    public HttpClient? OcelotClient => ocelotClient;

    protected virtual FileHostAndPort /*object?*/ Localhost(int port)
    {
        // return Ocelot.CreateFileHostAndPort("localhost", port, out _);
        return new("localhost", port);
    }

    protected static string DownstreamUrl(int port) => DownstreamUrl(port, Uri.UriSchemeHttp);
    protected static string DownstreamUrl(int port, string scheme) => $"{scheme ?? Uri.UriSchemeHttp}://localhost:{port}";
    protected static string LoopbackLocalhostUrl(int port, int loopbackIndex = 0) => $"{Uri.UriSchemeHttp}://127.0.0.{++loopbackIndex}:{port}";

    protected virtual FileConfiguration /*object?*/ GivenConfiguration(params FileRoute[] /*object[]*/ routes)
    {
        //object? c = Ocelot.CreateFileConfiguration(out Type type);
        //PropertyInfo? property = type.GetProperty("Routes");
        //if (property?.GetValue(c) is IList list)
        //    foreach (var route in routes)
        //        list.Add(route);
        //return c;
        var c = new FileConfiguration();
        c.Routes.AddRange(routes);
        return c;
    }

    protected virtual FileRoute GivenDefaultRoute(int port) => GivenRoute(port);
    protected virtual FileRoute GivenCatchAllRoute(int port) => GivenRoute(port, "/{everything}", "/{everything}");
    protected virtual FileRoute GivenRoute(int port, string? upstream = null, string? downstream = null)
    {
        // object? r = Ocelot.CreateFileRoute(out Type type);
        var r = new FileRoute();

        r.DownstreamHostAndPorts.Add(Localhost(port));
        //PropertyInfo? property = type.GetProperty("DownstreamHostAndPorts");
        //if (property?.GetValue(r) is IList downstreamHostAndPorts)
        //    downstreamHostAndPorts.Add(Localhost(port));

        r.DownstreamPathTemplate = downstream ?? "/";
        //property = type.GetProperty("DownstreamPathTemplate");
        //property?.SetValue(r, downstream ?? "/");

        r.DownstreamScheme = Uri.UriSchemeHttp;
        //property = type.GetProperty("DownstreamScheme");
        //property?.SetValue(r, Uri.UriSchemeHttp);

        r.UpstreamPathTemplate = upstream ?? "/";
        //property = type.GetProperty("UpstreamPathTemplate");
        //property?.SetValue(r, upstream ?? "/");

        r.UpstreamHttpMethod.Add(HttpMethods.Get);
        //property = type.GetProperty("UpstreamHttpMethod");
        //if (property?.GetValue(r) is IList upstreamHttpMethod)
        //    upstreamHttpMethod.Add(HttpMethods.Get);

        return r;
    }

    public virtual void GivenThereIsAConfiguration(FileConfiguration configuration)
        => GivenThereIsAConfiguration(configuration, ocelotConfigFileName);
    public virtual void GivenThereIsAConfiguration(FileConfiguration from, string toFile)
    {
        var json = SerializeJson(from, ref toFile);
        File.WriteAllText(toFile, json);
    }
    public virtual Task GivenThereIsAConfigurationAsync(FileConfiguration from, string toFile)
    {
        var json = SerializeJson(from, ref toFile);
        return File.WriteAllTextAsync(toFile, json);
    }
    protected virtual string SerializeJson(FileConfiguration from, ref string toFile)
    {
        toFile ??= ocelotConfigFileName;
        Files.Add(toFile); // register for disposing
        return JsonSerializer.Serialize(from, JsonWebIndented /*Formatting.Indented*/);
    }

    public readonly static JsonSerializerOptions JsonWebIndented = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // Avoid escaping non-ASCII
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,     // Use camelCase for web
        WriteIndented = true,                                  // Not compact output for better readability
        PropertyNameCaseInsensitive = true                     // Optional: for deserialization
    };

    #region GivenOcelotIsRunning
    public void WithBasicConfiguration(HostBuilderContext hosting, IConfigurationBuilder config)
    {
        config.SetBasePath(hosting.HostingEnvironment.ContentRootPath);
        config.AddOcelot(ocelotConfigFileName, false, false);
        //config.AddJsonFile(ocelotConfigFileName, false, false);
    }
    public void WithBasicConfiguration(WebHostBuilderContext hosting, IConfigurationBuilder config)
    {
        config.SetBasePath(hosting.HostingEnvironment.ContentRootPath);
        config.AddOcelot(ocelotConfigFileName, false, false);
        //config.AddJsonFile(ocelotConfigFileName, false, false);
    }

    public static void WithAddOcelot(IServiceCollection services) => Ocelot.AddOcelot(services); // services.AddOcelot();
    public static void WithUseOcelot(IApplicationBuilder app) => WithUseOcelotAsync(app).GetAwaiter().GetResult();
    public static Task<IApplicationBuilder> WithUseOcelotAsync(IApplicationBuilder app) => Ocelot.UseOcelot(app); // app.UseOcelot();

    public int GivenOcelotIsRunning()
        => GivenOcelotIsRunning(null, null, null, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync()
        => GivenOcelotIsRunningAsync(null, null, null, null, null, null, null);

    public int GivenOcelotIsRunning(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        => GivenOcelotIsRunning(configureDelegate, null, null, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        => GivenOcelotIsRunningAsync(configureDelegate, null, null, null, null, null, null);

    public int GivenOcelotIsRunning(Action<IServiceCollection> configureServices)
        => GivenOcelotIsRunning(null, configureServices, null, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync(Action<IServiceCollection> configureServices)
        => GivenOcelotIsRunningAsync(null, configureServices, null, null, null, null, null);

    public int GivenOcelotIsRunning(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, Action<IServiceCollection> configureServices)
        => GivenOcelotIsRunning(configureDelegate, configureServices, null, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, Action<IServiceCollection> configureServices)
        => GivenOcelotIsRunningAsync(configureDelegate, configureServices, null, null, null, null, null);

    public int GivenOcelotIsRunning(Action<IApplicationBuilder>? configureApp)
        => GivenOcelotIsRunning(null, null, configureApp, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync(Action<IApplicationBuilder>? configureApp)
        => GivenOcelotIsRunningAsync(null, null, configureApp, null, null, null, null);

    public int GivenOcelotIsRunning(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, Action<IServiceCollection> configureServices, Action<IApplicationBuilder>? configureApp)
        => GivenOcelotIsRunning(configureDelegate, configureServices, configureApp, null, null, null, null);
    public Task<int> GivenOcelotIsRunningAsync(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, Action<IServiceCollection> configureServices, Action<IApplicationBuilder>? configureApp)
        => GivenOcelotIsRunningAsync(configureDelegate, configureServices, configureApp, null, null, null, null);

    protected int GivenOcelotIsRunning(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost, Action<IWebHostBuilder>? postConfigureHost,
        Action<TestServer>? configureServer,
        Action<HttpClient>? configureClient)
    {
#if NET10_0_OR_GREATER
        return GivenOcelotHostIsRunning(configureDelegate, configureServices, configureApp, configureWebHost, postConfigureHost, configureServer, configureClient)
            .GetAwaiter().GetResult();
#else
        return GivenOcelotIsRunningInternal(configureDelegate, configureServices, configureApp, configureWebHost, postConfigureHost, configureServer, configureClient);
#endif
    }

    protected Task<int> GivenOcelotIsRunningAsync(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost, Action<IWebHostBuilder>? postConfigureHost,
        Action<TestServer>? configureServer,
        Action<HttpClient>? configureClient)
    {
#if NET10_0_OR_GREATER
        return GivenOcelotHostIsRunning(configureDelegate, configureServices, configureApp, configureWebHost, postConfigureHost, configureServer, configureClient);
#else
        return Task.Run(() => GivenOcelotIsRunningInternal(configureDelegate, configureServices, configureApp, configureWebHost, postConfigureHost, configureServer, configureClient));
#endif
    }

#if (NET8_0 || NET9_0)
    private int GivenOcelotIsRunningInternal(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? сonfigureWebHost, Action<IWebHostBuilder>? postConfigureHost,
        Action<TestServer>? configureServer,
        Action<HttpClient>? configureClient)
    {
        int port = PortFinder.GetRandomPort();
        var baseUrl = DownstreamUrl(port);
        var builder = TestHostBuilder.Create();
        if (сonfigureWebHost is not null)
            сonfigureWebHost(builder);
        else builder
            .ConfigureAppConfiguration(configureDelegate ?? WithBasicConfiguration)
            .ConfigureServices(configureServices ?? WithAddOcelot)
            .Configure(configureApp ?? WithUseOcelot)
            .UseUrls(baseUrl); // run Ocelot on specific port, rather than on std 80 port of TestServer
        postConfigureHost?.Invoke(builder);

        ocelotServer = new(builder)
        {
            BaseAddress = new(baseUrl) // will create Oc client with this base address, including port
        };
        configureServer?.Invoke(ocelotServer);
        ocelotClient = ocelotServer.CreateClient();
        configureClient?.Invoke(ocelotClient);
        return port;
    }
#endif
    private static void SetBaseUrl(FileConfiguration configuration, string baseUrl)
    {
        configuration.GlobalConfiguration.BaseUrl = baseUrl;
        //var p1 = configuration?.GetType().GetProperty("GlobalConfiguration");
        //var globalConfiguration = p1?.GetValue(configuration);
        //var p2 = p1?.PropertyType.GetProperty("BaseUrl");
        //p2?.SetValue(globalConfiguration, baseUrl);
    }

    protected async Task<int> GivenOcelotHostIsRunning(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? сonfigureWebHost, Action<IWebHostBuilder>? postConfigureHost,
        Action<TestServer>? configureServer,
        Action<HttpClient>? configureClient)
    {
        int port = PortFinder.GetRandomPort();
        var baseUrl = DownstreamUrl(port);
        void ConfigureWeb(IWebHostBuilder builder)
        {
            builder
                .UseKestrel()
                .ConfigureAppConfiguration(configureDelegate ?? WithBasicConfiguration)
                .ConfigureServices(configureServices ?? WithAddOcelot)
                .Configure(configureApp ?? WithUseOcelot)
                .UseUrls(baseUrl);
                //.UseTestServer(o => o.BaseAddress = new(baseUrl));
            postConfigureHost?.Invoke(builder);
        }
        var host = TestHostBuilder
            .CreateHost()
            .ConfigureWebHost(сonfigureWebHost ?? ConfigureWeb)
            .Build();
        await host.StartAsync();
        ocelotHost = host;
        //ocelotServer = host.GetTestServer();
        //configureServer?.Invoke(ocelotServer!);
        ocelotClient = ocelotServer?.CreateClient();
        ocelotClient ??= new() { BaseAddress = new(baseUrl), };
        configureClient?.Invoke(ocelotClient);
        return port;
    }

    protected IServiceProvider OcelotServices { get => ocelotServer?.Services ?? ocelotHost!.Services; }
    #endregion

    public static void GivenIWait(int wait) => Thread.Sleep(wait);

    #region Cookies

    public void GivenIAddCookieToMyRequest(string cookie)
        => ocelotClient.ShouldNotBeNull().DefaultRequestHeaders.Add("Set-Cookie", cookie);
    public async Task WhenIGetUrlOnTheApiGatewayWithCookie(string url, string cookie, string value)
        => response = await WhenIGetUrlOnTheApiGateway(url, cookie, value);
    public async Task WhenIGetUrlOnTheApiGatewayWithCookie(string url, CookieHeaderValue cookie)
        => response = await WhenIGetUrlOnTheApiGateway(url, cookie);
    public Task<HttpResponseMessage> WhenIGetUrlOnTheApiGateway(string url, string cookie, string value)
        => WhenIGetUrlOnTheApiGateway(url, new CookieHeaderValue(cookie, value));
    public Task<HttpResponseMessage> WhenIGetUrlOnTheApiGateway(string url, CookieHeaderValue cookie)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("Cookie", cookie.ToString());
        return ocelotClient.ShouldNotBeNull().SendAsync(requestMessage);
    }
    #endregion

    #region Headers

    public void ThenTheResponseHeaderIs(string key, string value)
    {
        ThenTheResponseHeaderExists(key);
        var header = response?.Headers.GetValues(key) ?? [];
        header.Any(string.IsNullOrEmpty).ShouldBeFalse();
        string.Join(';', header).ShouldBe(value);
    }

    public void ThenTheResponseContentHeaderIs(string key, string value)
    {
        ThenTheResponseContentHeaderExists(key);
        var header = response?.Content.Headers.GetValues(key) ?? [];
        header.Any(string.IsNullOrEmpty).ShouldBeFalse();
        string.Join(';', header).ShouldBe(value);
    }

    public string ThenTheResponseHeaderExists(string key)
    {
        response.ShouldNotBeNull().Headers.Contains(key).ShouldBeTrue();
        var header = response.Headers.GetValues(key);
        return string.Join(';', header);
    }

    public void ThenTheResponseHeaderExists(string key, bool exists)
        => response.ShouldNotBeNull().Headers.Contains(key).ShouldBe(exists);

    public string ThenTheResponseContentHeaderExists(string key)
    {
        response.ShouldNotBeNull().Content.Headers.Contains(key).ShouldBeTrue();
        var header = response.Content.Headers.GetValues(key);
        return string.Join(';', header);
    }

    public void ThenTheResponseContentHeaderExists(string key, bool exists)
        => response.ShouldNotBeNull().Content.Headers.Contains(key).ShouldBe(exists);
    #endregion

    public void ThenTheResponseReasonPhraseIs(string expected)
        => response.ShouldNotBeNull().ReasonPhrase.ShouldBe(expected);

    public void GivenIHaveAddedATokenToMyRequest(string token, string scheme = "Bearer")
    {
        ArgumentNullException.ThrowIfNull(ocelotClient);
        ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, token);
    }

    public async Task WhenIGetUrlOnTheApiGateway(string url)
        => response = await ocelotClient.ShouldNotBeNull().GetAsync(url);

    public Task<HttpResponseMessage> WhenIGetUrl(string url)
        => ocelotClient.ShouldNotBeNull().GetAsync(url);

    public async Task WhenIGetUrlOnTheApiGatewayWithBody(string url, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = new StringContent(body),
        };
        response = await ocelotClient.ShouldNotBeNull().SendAsync(request);
    }

    public async Task WhenIGetUrlOnTheApiGatewayWithForm(string url, string name, IEnumerable<KeyValuePair<string, string>> values)
    {
        var content = new MultipartFormDataContent();
        var dataContent = new FormUrlEncodedContent(values);
        content.Add(dataContent, name);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = content,
        };
        ArgumentNullException.ThrowIfNull(ocelotClient);
        response = await ocelotClient.SendAsync(request);
    }

    public async Task WhenIGetUrlOnTheApiGateway(string url, HttpContent content)
    {
        ArgumentNullException.ThrowIfNull(ocelotClient);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url) { Content = content };
        response = await ocelotClient.SendAsync(httpRequestMessage);
    }

    public async Task WhenIPostUrlOnTheApiGateway(string url, HttpContent content)
    {
        ArgumentNullException.ThrowIfNull(ocelotClient);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        response = await ocelotClient.SendAsync(request);
    }
    public async Task WhenIPostUrlOnTheApiGateway(string url, string content)
    {
        ArgumentNullException.ThrowIfNull(ocelotClient);
        var postContent = new StringContent(content);
        response = await ocelotClient.PostAsync(url, postContent);
    }
    public async Task WhenIPostUrlOnTheApiGateway(string url, string content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(ocelotClient);
        var postContent = new StringContent(content, new MediaTypeHeaderValue(contentType));
        response = await ocelotClient.PostAsync(url, postContent);
    }
    public async Task WhenIDeleteUrlOnTheApiGateway(string url)
        => response = await ocelotClient.ShouldNotBeNull().DeleteAsync(url);

    public void GivenIAddAHeader(string key, string value)
    {
        key.ShouldNotBeNullOrEmpty();
        value.ShouldNotBeNullOrEmpty();
        ocelotClient.ShouldNotBeNull().DefaultRequestHeaders.TryAddWithoutValidation(key, value);
    }

    public static void WhenIDoActionMultipleTimes(int times, Action<int> action)
    {
        for (int i = 0; i < times; i++)
            action?.Invoke(i);
    }

    public static async Task WhenIDoActionMultipleTimes(int times, Func<int, Task> action)
    {
        for (int i = 0; i < times; i++)
            await action.Invoke(i);
    }
    public static async Task WhenIDoActionForTime(TimeSpan time, Func<int, Task> action)
    {
        var watcher = Stopwatch.StartNew();
        for (int i = 0; watcher.Elapsed < time; i++)
        {
            await action.Invoke(i);
        }
        watcher.Stop();
    }

    public void ThenTheResponseBody([CallerMemberName] string testName = "")
        => ThenTheResponseBodyShouldBe(testName);
    public Task ThenTheResponseBodyAsync([CallerMemberName] string testName = "")
        => ThenTheResponseBodyShouldBeAsync(testName);

    public void ThenTheResponseBodyShouldBe(string expectedBody)
        => response.ShouldNotBeNull()
        .Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody);
    public Task ThenTheResponseBodyShouldBeAsync(string expectedBody)
        => response.ShouldNotBeNull()
        .Content.ReadAsStringAsync()
        .ContinueWith(t => t.Result.ShouldBe(expectedBody));

    public void ThenTheResponseBodyShouldBe(string expectedBody, string customMessage)
        => response.ShouldNotBeNull()
        .Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody, customMessage);
    public Task ThenTheResponseBodyShouldBeAsync(string expectedBody, string customMessage)
        => response.ShouldNotBeNull()
        .Content.ReadAsStringAsync()
        .ContinueWith(t => t.Result.ShouldBe(expectedBody, customMessage));

    public void ThenTheContentLengthIs(int expected)
        => response.ShouldNotBeNull().Content.Headers.ContentLength.ShouldBe(expected);

    public void ThenTheStatusCodeShouldBeOK()
        => ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    public void ThenTheStatusCodeShouldBe(HttpStatusCode expected)
        => response.ShouldNotBeNull().StatusCode.ShouldBe(expected);
    public void ThenTheStatusCodeShouldBe(int expected)
        => ((int)response.ShouldNotBeNull().StatusCode).ShouldBe(expected);

    public Task ReleasePortAsync(params int[] ports)
        => handler.ReleasePortAsync(ports);

    #region Dispose pattern

    /// <summary>
    /// Public implementation of Dispose pattern callable by consumers.
    /// </summary>
    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposedValue;

    /// <summary>Protected implementation of Dispose pattern.</summary>
    /// <param name="disposing">Flag to trigger actual disposing operation.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            ocelotClient?.Dispose();
            ocelotServer?.Dispose();
            ocelotHost?.Dispose();
            response?.Dispose();
            handler.Dispose();
            DeleteFiles();
            DeleteFolders();
        }

        _disposedValue = true;
    }

    protected virtual void DeleteFiles()
    {
        foreach (var file in Files)
        {
            if (!File.Exists(file))
                continue;

            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        Files.Clear();
    }

    protected virtual void DeleteFolders()
    {
        foreach (var folder in Folders)
        {
            try
            {
                var f = new DirectoryInfo(folder);
                if (f.Exists && f.FullName != AppContext.BaseDirectory)
                {
                    f.Delete(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        Folders.Clear();
    }
    #endregion
}

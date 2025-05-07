using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shouldly;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using CookieHeaderValue = Microsoft.Net.Http.Headers.CookieHeaderValue;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Ocelot.Testing;

public class AcceptanceSteps : IDisposable
{
    protected IWebHost? ocelotHost;
    protected TestServer? ocelotServer;
    protected HttpClient? ocelotClient;
    protected HttpResponseMessage? response;

    private readonly Guid _testId;
    protected readonly Random random;
    protected readonly string ocelotConfigFileName;

    public AcceptanceSteps() : base()
    {
        _testId = Guid.NewGuid();
        random = new Random();
        ocelotConfigFileName = $"{_testId:N}-{ConfigurationBuilderExtensions.PrimaryConfigFile}";
        Files = new() { ocelotConfigFileName };
        Folders = new();
    }

    protected List<string> Files { get; }
    protected List<string> Folders { get; }
    protected string TestID { get => _testId.ToString("N"); }

    protected static FileHostAndPort Localhost(int port) => new("localhost", port);
    protected static string DownstreamUrl(int port) => $"{Uri.UriSchemeHttp}://localhost:{port}";
    protected static string LoopbackLocalhostUrl(int port, int loopbackIndex = 0) => $"{Uri.UriSchemeHttp}://127.0.0.{++loopbackIndex}:{port}";

    protected static FileConfiguration GivenConfiguration(params FileRoute[] routes) => new()
    {
        Routes = new(routes),
    };

    protected static FileRoute GivenDefaultRoute(int port) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamHostAndPorts = [ Localhost(port) ],
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = [ HttpMethods.Get ],
    };

    public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        => GivenThereIsAConfiguration(fileConfiguration, ocelotConfigFileName);
    public void GivenThereIsAConfiguration(FileConfiguration from, string toFile)
    {
        var json = SerializeJson(from, ref toFile);
        File.WriteAllText(toFile, json);
    }
    public Task GivenThereIsAConfigurationAsync(FileConfiguration from, string toFile)
    {
        var json = SerializeJson(from, ref toFile);
        return File.WriteAllTextAsync(toFile, json);
    }
    protected string SerializeJson(FileConfiguration from, ref string toFile)
    {
        toFile ??= ocelotConfigFileName;
        Files.Add(toFile); // register for disposing
        return JsonConvert.SerializeObject(from, Formatting.Indented);
    }

    #region GivenOcelotIsRunning
    public void WithBasicConfiguration(WebHostBuilderContext hosting, IConfigurationBuilder config) => config
        .SetBasePath(hosting.HostingEnvironment.ContentRootPath)
        .AddOcelot(ocelotConfigFileName, false, false);
    public static void WithAddOcelot(IServiceCollection services) => services.AddOcelot();
    public static void WithUseOcelot(IApplicationBuilder app) => app.UseOcelot().Wait();
    public static Task<IApplicationBuilder> WithUseOcelotAsync(IApplicationBuilder app) => app.UseOcelot();

    public int GivenOcelotIsRunning()
        => GivenOcelotIsRunning(null, null, null, null, null, null);
    public int GivenOcelotIsRunning(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        => GivenOcelotIsRunning(configureDelegate, null, null, null, null, null);
    public int GivenOcelotIsRunning(Action<IServiceCollection> configureServices)
        => GivenOcelotIsRunning(null, configureServices, null, null, null, null);

    protected int GivenOcelotIsRunning(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureWebHost,
        Action<TestServer>? configureServer,
        Action<HttpClient>? configureClient)
    {
        int port = PortFinder.GetRandomPort();
        var baseUrl = DownstreamUrl(port);
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration(configureDelegate ?? WithBasicConfiguration)
            .ConfigureServices(configureServices ?? WithAddOcelot)
            .Configure(configureApp ?? WithUseOcelot)
            .UseUrls(baseUrl); // run Ocelot on specific port, rather than on std 80 port of TestServer
        configureWebHost?.Invoke(builder);
        ocelotServer = new(builder)
        {
            BaseAddress = new(baseUrl) // will create Oc client with this base address, including port
        };
        configureServer?.Invoke(ocelotServer);
        ocelotClient = ocelotServer.CreateClient();
        configureClient?.Invoke(ocelotClient);
        return port;
    }

    protected async Task<IHost> GivenOcelotHostIsRunning(
        Action<WebHostBuilderContext, IConfigurationBuilder>? configureDelegate,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configureApp,
        Action<IWebHostBuilder>? configureHost)
    {
        void ConfigureWeb(IWebHostBuilder webBuilder)
        {
            webBuilder
                .UseKestrel()
                .ConfigureAppConfiguration(configureDelegate ?? WithBasicConfiguration)
                .ConfigureServices(configureServices ?? WithAddOcelot)
                .Configure(configureApp ?? WithUseOcelot);
            configureHost?.Invoke(webBuilder);
        }
        var host = TestHostBuilder
            .CreateHost()
            .ConfigureWebHost(ConfigureWeb)
            .Build();
        await host.StartAsync();
        return host;
    }
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

    public void ThenTheResponseHeaderIs(string key, string value) => ThenTheResponseHeaderExists(key).First().ShouldBe(value);
    public void ThenTheResponseContentHeaderIs(string key, string value) => ThenTheResponseContentHeaderExists(key).First().ShouldBe(value);

    public IEnumerable<string> ThenTheResponseHeaderExists(string key)
    {
        response.ShouldNotBeNull();
        response.Headers.Contains(key).ShouldBeTrue();
        var header = response.Headers.GetValues(key);
        header.Any(string.IsNullOrEmpty).ShouldBeFalse();
        return header;
    }
    public IEnumerable<string> ThenTheResponseContentHeaderExists(string key)
    {
        response.ShouldNotBeNull().Content.Headers.Contains(key).ShouldBeTrue();
        var header = response.Content.Headers.GetValues(key);
        header.Any(string.IsNullOrEmpty).ShouldBeFalse();
        return header;
    }
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
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        response = await ocelotClient.SendAsync(httpRequestMessage);
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

    public void ThenTheResponseBodyShouldBe(string expectedBody)
        => response.ShouldNotBeNull().Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody);
    public async Task ThenTheResponseBodyShouldBeAsync(string expectedBody)
    {
        response.ShouldNotBeNull();
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBe(expectedBody);
    }

    public void ThenTheResponseBodyShouldBe(string expectedBody, string customMessage)
        => response.ShouldNotBeNull().Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody, customMessage);
    public async Task ThenTheResponseBodyShouldBeAsync(string expectedBody, string customMessage)
    {
        response.ShouldNotBeNull();
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBe(expectedBody, customMessage);
    }

    public void ThenTheContentLengthIs(int expected)
        => response.ShouldNotBeNull().Content.Headers.ContentLength.ShouldBe(expected);

    public void ThenTheStatusCodeShouldBe(HttpStatusCode expected)
        => response.ShouldNotBeNull().StatusCode.ShouldBe(expected);
    public void ThenTheStatusCodeShouldBe(int expected)
        => ((int)response.ShouldNotBeNull().StatusCode).ShouldBe(expected);

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
    }
    #endregion
}

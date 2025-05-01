//using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.Caching;
using Ocelot.AcceptanceTests.Properties;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using Ocelot.Provider.Polly;
using Ocelot.Tracing.Butterfly;
using Ocelot.Tracing.OpenTracing;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using CookieHeaderValue = Microsoft.Net.Http.Headers.CookieHeaderValue;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    //protected TestServer _ocelotServer;
    //protected HttpClient _ocelotClient;
    //protected HttpResponseMessage _response;
    //private HttpContent _postContent;
    private BearerToken _token;

    //public string RequestIdKey = "OcRequestId";
    //private readonly Random _random;
    //protected readonly Guid _testId;
    //protected readonly string _ocelotConfigFileName;

    //// TODO Merge both members
    //protected IWebHostBuilder _webHostBuilder;
    //protected IWebHostBuilder _ocelotBuilder;
    //private IWebHost _ocelotHost; // TODO remove because of one reference
    public Steps() : base()
    {
        //_random = new Random();
        //_testId = Guid.NewGuid();
        //_ocelotConfigFileName = $"{_testId:N}-{ConfigurationBuilderExtensions.PrimaryConfigFile}";
        //Files = new() { _ocelotConfigFileName };
        //Folders = new();
        BddfyConfig.Configure();
    }

    //protected List<string> Files { get; }
    //protected List<string> Folders { get; }
    //protected string TestID { get => _testId.ToString("N"); }

    //protected static FileHostAndPort Localhost(int port) => new("localhost", port);
    //protected static string DownstreamUrl(int port) => $"{Uri.UriSchemeHttp}://localhost:{port}";
    //protected static string LoopbackLocalhostUrl(int port, int loopbackIndex = 0) => $"{Uri.UriSchemeHttp}://127.0.0.{++loopbackIndex}:{port}";

    //protected static FileConfiguration GivenConfiguration(params FileRoute[] routes) => new()
    //{
    //    Routes = new(routes),
    //};

    //protected static FileRoute GivenDefaultRoute(int port) => new()
    //{
    //    DownstreamPathTemplate = "/",
    //    DownstreamHostAndPorts = new() { Localhost(port) },
    //    DownstreamScheme = Uri.UriSchemeHttp,
    //    UpstreamPathTemplate = "/",
    //    UpstreamHttpMethod = new() { HttpMethods.Get },
    //};
    //public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
    //    => GivenThereIsAConfiguration(fileConfiguration, _ocelotConfigFileName);
    //public void GivenThereIsAConfiguration(FileConfiguration from, string toFile)
    //{
    //    var json = SerializeJson(from, ref toFile);
    //    File.WriteAllText(toFile, json);
    //}
    //public Task GivenThereIsAConfigurationAsync(FileConfiguration from, string toFile)
    //{
    //    var json = SerializeJson(from, ref toFile);
    //    return File.WriteAllTextAsync(toFile, json);
    //}
    //protected string SerializeJson(FileConfiguration from, ref string toFile)
    //{
    //    toFile ??= _ocelotConfigFileName;
    //    Files.Add(toFile); // register for disposing
    //    return JsonConvert.SerializeObject(from, Formatting.Indented);
    //}

    //protected virtual void DeleteFiles()
    //{
    //    foreach (var file in Files)
    //    {
    //        if (!File.Exists(file))
    //        {
    //            continue;
    //        }

    //        try
    //        {
    //            File.Delete(file);
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //        }
    //    }
    //}

    //protected virtual void DeleteFolders()
    //{
    //    foreach (var folder in Folders)
    //    {
    //        try
    //        {
    //            var f = new DirectoryInfo(folder);
    //            if (f.Exists && f.FullName != AppContext.BaseDirectory)
    //            {
    //                f.Delete(true);
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //        }
    //    }
    //}
    public void ThenTheResponseBodyHeaderIs(string key, string value)
    {
        var header = _response.Content.Headers.GetValues(key);
        header.First().ShouldBe(value);
    }

    //protected void StartOcelot(Action<WebHostBuilderContext, IConfigurationBuilder> configureAddOcelot, string environmentName = null)
    //{
    //    _webHostBuilder = TestHostBuilder.Create()
    //        .ConfigureAppConfiguration((hostingContext, config) =>
    //        {
    //            var env = hostingContext.HostingEnvironment;
    //            config.SetBasePath(env.ContentRootPath);
    //            config.AddJsonFile("appsettings.json", true, false)
    //                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
    //            configureAddOcelot.Invoke(hostingContext, config); // config.AddOcelot(...);
    //            config.AddEnvironmentVariables();
    //        })
    //        .ConfigureServices(WithAddOcelot)
    //        .Configure(WithUseOcelot)
    //        .UseEnvironment(environmentName ?? nameof(AcceptanceTests));
    //    _ocelotServer = new TestServer(_webHostBuilder);
    //    _ocelotClient = _ocelotServer.CreateClient();
    //}
    public void ThenTheTraceHeaderIsSet(string key)
    {
        var header = _response.Headers.GetValues(key);
        header.First().ShouldNotBeNullOrEmpty();
    }

    //public static void GivenIWait(int wait) => Thread.Sleep(wait);
    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
    {
        GivenOcelotIsRunningWithServices(s => s
            .AddOcelot()
            .AddDelegatingHandler<THandler>(global));
    }

    // #
    // # Cookies helpers
    // #
    //public void GivenIAddCookieToMyRequest(string cookie)
    //    => _ocelotClient.DefaultRequestHeaders.Add("Set-Cookie", cookie);
    //public async Task WhenIGetUrlOnTheApiGatewayWithCookie(string url, string cookie, string value)
    //    => _response = await WhenIGetUrlOnTheApiGatewayWithRequestId(url, cookie, value);
    //public async Task WhenIGetUrlOnTheApiGatewayWithCookie(string url, CookieHeaderValue cookie)
    //    => _response = await WhenIGetUrlOnTheApiGatewayWithRequestId(url, cookie);
    //public Task<HttpResponseMessage> WhenIGetUrlOnTheApiGatewayWithRequestId(string url, string cookie, string value)
    //{
    //    var header = new CookieHeaderValue(cookie, value);
    //    return WhenIGetUrlOnTheApiGatewayWithRequestId(url, header);
    //}
    //public Task<HttpResponseMessage> WhenIGetUrlOnTheApiGatewayWithRequestId(string url, CookieHeaderValue cookie)
    //{
    //    var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
    //    requestMessage.Headers.Add("Cookie", cookie.ToString());
    //    return _ocelotClient.SendAsync(requestMessage);
    //}
    // END of Cookies helpers

    ///// <summary>
    ///// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
    ///// </summary>
    //public void GivenOcelotIsRunning(Action<IdentityServerAuthenticationOptions> options,
    //    string authenticationProviderKey)
    //{
    //    _webHostBuilder = TestHostBuilder.Create()
    //        .ConfigureAppConfiguration((hostingContext, config) =>
    //        {
    //            config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
    //            var env = hostingContext.HostingEnvironment;
    //            config.AddJsonFile("appsettings.json", true, false)
    //                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
    //            config.AddJsonFile(_ocelotConfigFileName, true, false);
    //            config.AddEnvironmentVariables();
    //        })
    //        .ConfigureServices(s =>
    //        {
    //            s.AddOcelot();
    //            s.AddAuthentication()
    //                .AddIdentityServerAuthentication(authenticationProviderKey, options);
    //        })
    //        .Configure(async app => await app.UseOcelot());
    //    _ocelotServer = new TestServer(_webHostBuilder);
    //    _ocelotClient = _ocelotServer.CreateClient();
    //}
    //public void ThenTheResponseHeaderIs(string key, string value)
    //{
    //    var header = _response.Headers.GetValues(key);
    //    header.First().ShouldBe(value);
    //}
    //public void ThenTheReasonPhraseIs(string expected)
    //{
    //    _response.ReasonPhrase.ShouldBe(expected);
    //}

    //public void GivenOcelotIsRunningWithServices(Action<IServiceCollection> configureServices)
    //    => GivenOcelotIsRunningWithServices(configureServices, null);
    //public void GivenOcelotIsRunningWithServices(Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configureApp/*, bool validateScopes*/)
    //{
    //    var builder = TestHostBuilder.Create() // ValidateScopes = true
    //        .ConfigureAppConfiguration(WithBasicConfiguration)
    //        .ConfigureServices(configureServices ?? WithAddOcelot)
    //        .Configure(configureApp ?? WithUseOcelot);
    //    _ocelotServer = new TestServer(builder);
    //    _ocelotClient = _ocelotServer.CreateClient();
    //}
    //public static void WithAddOcelot(IServiceCollection services) => services.AddOcelot();
    //public static void WithUseOcelot(IApplicationBuilder app) => app.UseOcelot().Wait();

    /// <summary>
    /// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
    /// </summary>
    public void GivenOcelotIsRunning(OcelotPipelineConfiguration pipelineConfig)
    {
        var builder = TestHostBuilder.Create() // ValidateScopes = true
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async a => await a.UseOcelot(pipelineConfig));

        //var configuration = _webHostBuilder.Build();
        //_webHostBuilder = TestHostBuilder.Create()
        //    .ConfigureServices(s => { s.AddSingleton(_webHostBuilder); });
        //_ocelotServer = new TestServer(_webHostBuilder
        //    .UseConfiguration(configuration)
        //    .ConfigureServices(s => { s.AddOcelot(configuration); })
        //    .ConfigureLogging(l =>
        //    {
        //        l.AddConsole();
        //        l.AddDebug();
        //    })
        //    .Configure(async a => await a.UseOcelot(ocelotPipelineConfig)));
        _ocelotServer = new TestServer(builder);
        _ocelotClient = _ocelotServer.CreateClient();
    }

    public void GivenIHaveAddedATokenToMyRequest()
    {
        _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
    }

    public static List<KeyValuePair<string, string>> GivenDefaultAuthTokenForm() => new()
    {
        new ("client_id", "client"),
        new ("client_secret", "secret"),
        new ("scope", "api"),
        new ("username", "test"),
        new ("password", "test"),
        new ("grant_type", "password"),
    };

    internal Task<BearerToken> GivenIHaveAToken(string url)
    {
        var form = GivenDefaultAuthTokenForm();
        return GivenIHaveATokenWithForm(url, form);
    }

    internal async Task<BearerToken> GivenIHaveATokenWithForm(string url, IEnumerable<KeyValuePair<string, string>> form)
    {
        var tokenUrl = $"{url}/connect/token";
        var formData = form ?? Enumerable.Empty<KeyValuePair<string, string>>();
        var content = new FormUrlEncodedContent(formData);

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
        return _token;
    }

    //public static async Task VerifyIdentityServerStarted(string url)
    //{
    //    using var httpClient = new HttpClient();
    //    var response = await httpClient.GetAsync($"{url}/.well-known/openid-configuration");
    //    await response.Content.ReadAsStringAsync();
    //    response.EnsureSuccessStatusCode();
    //}
    //public async Task WhenIGetUrlOnTheApiGatewayWithRequestId(string url)
    //    => _response = await _ocelotClient.GetAsync(url);
    //public Task<HttpResponseMessage> WhenIGetUrl(string url)
    //    => _ocelotClient.GetAsync(url);
    //public async Task WhenIGetUrlOnTheApiGatewayWithBody(string url, string body)
    //{
    //    var request = new HttpRequestMessage(HttpMethod.Get, url)
    //    {
    //        Content = new StringContent(body),
    //    };
    //    _response = await _ocelotClient.SendAsync(request);
    //}
    //public async Task WhenIGetUrlOnTheApiGatewayWithForm(string url, string name, IEnumerable<KeyValuePair<string, string>> values)
    //{
    //    var content = new MultipartFormDataContent();
    //    var dataContent = new FormUrlEncodedContent(values);
    //    content.Add(dataContent, name);
    //    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
    //    var request = new HttpRequestMessage(HttpMethod.Get, url)
    //    {
    //        Content = content,
    //    };
    //    _response = await _ocelotClient.SendAsync(request);
    //}
    //public async Task WhenIGetUrlOnTheApiGatewayWithRequestId(string url, HttpContent content)
    //{
    //    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url) { Content = content };
    //    _response = await _ocelotClient.SendAsync(httpRequestMessage);
    //}
    //public async Task WhenIPostUrlOnTheApiGateway(string url, HttpContent content)
    //{
    //    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
    //    _response = await _ocelotClient.SendAsync(httpRequestMessage);
    //}
    //public void GivenIAddAHeader(string key, string value)
    //{
    //    _ocelotClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
    //}
    //public static void WhenIDoActionMultipleTimes(int times, Action<int> action)
    //{
    //    for (int i = 0; i < times; i++)
    //        action?.Invoke(i);
    //}
    //public static async Task WhenIDoActionMultipleTimes(int times, Func<int, Task> action)
    //{
    //    for (int i = 0; i < times; i++)
    //        await action.Invoke(i);
    //}
    //public static async Task WhenIDoActionForTime(TimeSpan time, Func<int, Task> action)
    //{
    //    var watcher = Stopwatch.StartNew();
    //    for (int i = 0; watcher.Elapsed < time; i++)
    //    {
    //        await action.Invoke(i);
    //    }
    //    watcher.Stop();
    //}

    //public async Task WhenIGetUrlOnTheApiGatewayWithRequestId(string url, string requestId)
    //{
    //    _ocelotClient.DefaultRequestHeaders.TryAddWithoutValidation(RequestIdKey, requestId);
    //    _response = await _ocelotClient.GetAsync(url);
    //}
    //public async Task WhenIPostUrlOnTheApiGateway(string url)
    //{
    //    _response = await _ocelotClient.PostAsync(url, _postContent);
    //}

    //public void GivenThePostHasContent(string postContent)
    //{
    //    _postContent = new StringContent(postContent);
    //}

    //public void GivenThePostHasContentType(string postContent)
    //{
    //    _postContent.Headers.ContentType = new MediaTypeHeaderValue(postContent);
    //}

    //public void ThenTheResponseBodyShouldBe(string expectedBody)
    //    => _response.Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody);
    //public void ThenTheResponseBodyShouldBe(string expectedBody, string customMessage)
    //    => _response.Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedBody, customMessage);

    //public void ThenTheContentLengthIs(int expected)
    //    => _response.Content.Headers.ContentLength.ShouldBe(expected);

    //public void ThenTheStatusCodeShouldBe(HttpStatusCode expected)
    //    => _response.StatusCode.ShouldBe(expected);
    //public void ThenTheStatusCodeShouldBe(int expected)
    //    => ((int)_response.StatusCode).ShouldBe(expected);

    //public void ThenTheRequestIdIsReturned()
    //    => _response.Headers.GetValues(RequestIdKey).First().ShouldNotBeNullOrEmpty();

    //public void ThenTheRequestIdIsReturned(string expected)
    //    => _response.Headers.GetValues(RequestIdKey).First().ShouldBe(expected);
}

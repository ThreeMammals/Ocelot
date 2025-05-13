using CacheManager.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Ocelot.Cache.CacheManager;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocelot.AcceptanceTests.Caching;

public sealed class CachingTests : Steps
{
    private const string HelloTomContent = "Hello from Tom";
    private const string HelloLauraContent = "Hello from Laura";
    private int _counter = 0;

    public CachingTests()
    {
    }

    [Fact]
    public void Should_return_cached_response()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var configuration = GivenFileConfiguration(port, options);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => ThenTheContentLengthIs(HelloLauraContent.Length))
            .BDDfy();
    }

    [Fact]
    public void Should_return_cached_response_with_expires_header()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var configuration = GivenFileConfiguration(port, options);
        var headerExpires = "Expires";
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloLauraContent, headerExpires, "-1"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => ThenTheContentLengthIs(HelloLauraContent.Length))
            .And(x => ThenTheResponseContentHeaderIs(headerExpires, "-1"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_cached_response_when_using_jsonserialized_cache()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var configuration = GivenFileConfiguration(port, options);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingJsonSerializedCache())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingJsonSerializedCache()
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", true, false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
                config.AddJsonFile(ocelotConfigFileName, false, false);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(s =>
            {
                s.AddOcelot()
                    .AddCacheManager((x) =>
                    {
                        //x.WithMicrosoftLogging(_ => /*log.AddConsole(LogLevel.Debug);*/)
                        x.WithJsonSerializer();
                        x.WithHandle(typeof(InMemoryJsonHandle<>));
                    });
            })
            .Configure(async app => await app.UseOcelot());

        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }

    [Fact]
    public void Should_not_return_cached_response_as_ttl_expires()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 1,
        };
        var configuration = GivenFileConfiguration(port, options);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, null, null))
            .And(x => GivenTheCacheExpires())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloTomContent))
            .BDDfy();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void Should_return_different_cached_response_when_request_body_changes_and_EnableContentHashing_is_true(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            EnableContentHashing = true,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig);

        this.Given(x => x.GivenThereIsAnEchoServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody2String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody2String))
            .And(x => ThenTheCounterValueShouldBe(2))
            .BDDfy();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void Should_return_same_cached_response_when_request_body_changes_and_EnableContentHashing_is_false(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig);

        this.Given(x => x.GivenThereIsAnEchoServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(testBody1String))
            .And(x => ThenTheCounterValueShouldBe(1))
            .BDDfy();
    }

    [Fact]
    [Trait("Issue", "1172")]
    public void Should_clean_cached_response_by_cache_header_via_new_caching_key()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            Region = "europe-central",
            Header = "Authorization",
        };
        var configuration = GivenFileConfiguration(port, options);
        var headerExpires = "Expires";

        // Add to cache
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloLauraContent, headerExpires, options.TtlSeconds))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))

            // Read from cache
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, headerExpires, options.TtlSeconds / 2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => ThenTheContentLengthIs(HelloLauraContent.Length))

            // Clean cache by the header and cache new content
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloTomContent, headerExpires, -1))
            .And(x => GivenIAddAHeader(options.Header, "123"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloTomContent))
            .And(x => ThenTheContentLengthIs(HelloTomContent.Length))
            .BDDfy();
    }

    private static FileConfiguration GivenFileConfiguration(int port, FileCacheOptions cacheOptions, bool asGlobalConfig = false) => new()
    {
        Routes = new()
        {
            new FileRoute()
            {
                DownstreamPathTemplate = "/",
                DownstreamHostAndPorts = new()
                {
                    new FileHostAndPort("localhost", port),
                },
                DownstreamHttpMethod = "Post",
                DownstreamScheme = Uri.UriSchemeHttp,
                UpstreamPathTemplate = "/",
                UpstreamHttpMethod = new() { HttpMethods.Get, HttpMethods.Post },
                FileCacheOptions = asGlobalConfig ? new FileCacheOptions { TtlSeconds = cacheOptions.TtlSeconds } : cacheOptions,
            },
        },
        GlobalConfiguration = asGlobalConfig ? new FileGlobalConfiguration { CacheOptions = cacheOptions } : null,
    };

    private static void GivenTheCacheExpires()
    {
        Thread.Sleep(1000);
    }

    private void GivenTheServiceNowReturns(int port, HttpStatusCode statusCode, string responseBody, string key, object value)
    {
        handler.Dispose();
        GivenThereIsAServiceRunningOn(port, statusCode, responseBody, key, value);
    }

    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string responseBody, string key, object value)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                context.Response.Headers.Append(key, value.ToString());
            }

            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        });
    }

    private void GivenThereIsAnEchoServiceRunningOn(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            using var streamReader = new StreamReader(context.Request.Body);
            var requestBody = await streamReader.ReadToEndAsync();
            _counter++;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(requestBody);
        });
    }

    private void ThenTheCounterValueShouldBe(int expected)
    {
        Assert.Equal(expected, _counter);
    }

    private static (string TestBody1String, string TestBody2String) TestBodiesFactory()
    {
        var testBody1 = new TestBody
        {
            Age = 30,
            Email = "test.test@email.com",
            FirstName = "Jean",
            LastName = "Test",
        };

        var testBody1String = JsonSerializer.Serialize(testBody1);

        var testBody2 = new TestBody
        {
            Age = 31,
            Email = "test.test@email.com",
            FirstName = "Jean",
            LastName = "Test",
        };

        var testBody2String = JsonSerializer.Serialize(testBody2);

        return (testBody1String, testBody2String);
    }
}

public class TestBody
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

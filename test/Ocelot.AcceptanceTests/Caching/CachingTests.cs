using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocelot.AcceptanceTests.Caching;

public sealed class CachingTests : IDisposable
{
    private readonly Steps _steps;
    private readonly ServiceHandler _serviceHandler;

    private const string HelloTomContent = "Hello from Tom";
    private const string HelloLauraContent = "Hello from Laura";
    private int _counter = 0;

    public CachingTests()
    {
        _serviceHandler = new ServiceHandler();
        _steps = new Steps();
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

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => _steps.ThenTheContentLengthIs(HelloLauraContent.Length))
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
        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloLauraContent, headerExpires, "-1"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => _steps.ThenTheContentLengthIs(HelloLauraContent.Length))
            .And(x => _steps.ThenTheResponseBodyHeaderIs(headerExpires, "-1"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningUsingJsonSerializedCache())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, null, null))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .BDDfy();
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

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloLauraContent, null, null))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, null, null))
            .And(x => GivenTheCacheExpires())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloTomContent))
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

        this.Given(x => x.GivenThereIsAnEchoServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody2String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody2String))
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

        this.Given(x => x.GivenThereIsAnEchoServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, "application/json")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(testBody1String))
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
        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloLauraContent, headerExpires, options.TtlSeconds))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))

            // Read from cache
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, headerExpires, options.TtlSeconds / 2))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => _steps.ThenTheContentLengthIs(HelloLauraContent.Length))

            // Clean cache by the header and cache new content
            .Given(x => x.GivenTheServiceNowReturns($"http://localhost:{port}", HttpStatusCode.OK, HelloTomContent, headerExpires, -1))
            .And(x => _steps.GivenIAddAHeader(options.Header, "123"))
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(HelloTomContent))
            .And(x => _steps.ThenTheContentLengthIs(HelloTomContent.Length))
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

    private void GivenTheServiceNowReturns(string url, HttpStatusCode statusCode, string responseBody, string key, object value)
    {
        _serviceHandler.Dispose();
        GivenThereIsAServiceRunningOn(url, statusCode, responseBody, key, value);
    }

    private void GivenThereIsAServiceRunningOn(string url, HttpStatusCode statusCode, string responseBody, string key, object value)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                context.Response.Headers.Append(key, value.ToString());
            }

            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(responseBody);
        });
    }

    private void GivenThereIsAnEchoServiceRunningOn(string url)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
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

    private (string TestBody1String, string TestBody2String) TestBodiesFactory()
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

    public void Dispose()
    {
        _serviceHandler?.Dispose();
        _steps.Dispose();
    }
}

public class TestBody
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

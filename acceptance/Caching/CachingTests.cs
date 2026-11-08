using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Net.Mime;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocelot.Acceptance.Caching;

public sealed class CachingTests : Steps
{
    private const string HelloFromTom = "Hello from Tom";
    private const string HelloFromLaura = "Hello from Laura";
    private int _counter = 0;

    public CachingTests()
    {
    }

    [Fact]
    public async Task Should_return_cached_response()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var configuration = GivenFileConfiguration(port, options);

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, null, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);

        GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);
        ThenTheContentLengthIs(HelloFromLaura.Length);
    }

    [Fact]
    public async Task Should_return_cached_response_with_expires_header()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var configuration = GivenFileConfiguration(port, options);
        var headerExpires = "Expires";

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, headerExpires, "-1");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);

        GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);
        ThenTheContentLengthIs(HelloFromLaura.Length);
        ThenTheResponseContentHeaderIs(headerExpires, "-1");
    }

    [Fact]
    public async Task Should_not_return_cached_response_as_ttl_expires()
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 1,
        };
        var configuration = GivenFileConfiguration(port, options);

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, null, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);

        GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null);
        GivenTheCacheExpiresIn(1000);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromTom);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2058")] // https://github.com/ThreeMammals/Ocelot/pull/2058
    [Trait("Bug", "2059")] // https://github.com/ThreeMammals/Ocelot/issues/2059
    public async Task Should_return_different_cached_response_when_request_body_changes_and_EnableContentHashing_is_true(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            EnableContentHashing = true,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig, HttpMethods.Post);

        GivenThereIsAnEchoServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody2String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody2String);
        ThenTheCounterValueShouldBe(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2058")] // https://github.com/ThreeMammals/Ocelot/pull/2058
    [Trait("Bug", "2059")] // https://github.com/ThreeMammals/Ocelot/issues/2059
    public async Task Should_return_same_cached_response_when_request_body_changes_and_EnableContentHashing_is_false(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig, HttpMethods.Post);

        GivenThereIsAnEchoServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody1String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);

        await WhenIPostUrlOnTheApiGateway("/", new StringContent(testBody2String, Encoding.UTF8, MediaTypeNames.Application.Json));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(testBody1String);
        ThenTheCounterValueShouldBe(1);
    }

    [Fact]
    [Trait("Feat", "1172")] // https://github.com/ThreeMammals/Ocelot/pull/1172
    public async Task Should_clean_cached_response_by_cache_header_via_new_caching_key()
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
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, headerExpires, options.TtlSeconds);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);

        // Read from cache
        GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, headerExpires, options.TtlSeconds / 2);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromLaura);
        ThenTheContentLengthIs(HelloFromLaura.Length);

        // Clean cache by the header and cache new content
        GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, headerExpires, -1);
        GivenIAddAHeader(options.Header, "123");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(HelloFromTom);
        ThenTheContentLengthIs(HelloFromTom.Length);
    }

    private FileConfiguration GivenFileConfiguration(int port, FileCacheOptions cacheOptions,
        bool asGlobalConfig = false, params string[] methods)
    {
        var route = GivenRoute(port);
        route.CacheOptions = asGlobalConfig ? new() { TtlSeconds = cacheOptions.TtlSeconds } : cacheOptions;
        foreach (var m in methods)
            route.UpstreamHttpMethod.Add(m);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = !asGlobalConfig ? null :
            new()
            {
                CacheOptions = new(cacheOptions),
            };
        return configuration;
    }

    private static void GivenTheCacheExpiresIn(int ms) => GivenIWait(ms);

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1013:Public method should be marked as test", Justification = "Steps")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Steps")]
    public void GivenThereIsAnEchoServiceRunningOn(int port)
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

    public static (string TestBody1String, string TestBody2String) TestBodiesFactory()
    {
        var testBody1 = new TestBody
        {
            Age = 19,
            Email = "tom@ocelot.net",
            FirstName = "Tom",
            LastName = "Test",
        };

        var testBody1String = JsonSerializer.Serialize(testBody1);

        var testBody2 = new TestBody
        {
            Age = 25,
            Email = "laura@ocelot.net",
            FirstName = "Laura",
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

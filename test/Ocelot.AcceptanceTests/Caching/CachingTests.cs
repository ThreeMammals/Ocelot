using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocelot.AcceptanceTests.Caching;

public sealed class CachingTests : Steps
{
    private const string HelloFromTom = "Hello from Tom";
    private const string HelloFromLaura = "Hello from Laura";
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
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .And(x => ThenTheContentLengthIs(HelloFromLaura.Length))
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
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, headerExpires, "-1"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .And(x => ThenTheContentLengthIs(HelloFromLaura.Length))
            .And(x => ThenTheResponseContentHeaderIs(headerExpires, "-1"))
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
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, null, null))
            .And(x => GivenTheCacheExpiresIn(1000))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromTom))
        .BDDfy();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "2058")] // https://github.com/ThreeMammals/Ocelot/pull/2058
    [Trait("Bug", "2059")] // https://github.com/ThreeMammals/Ocelot/issues/2059
    public void Should_return_different_cached_response_when_request_body_changes_and_EnableContentHashing_is_true(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            EnableContentHashing = true,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig, HttpMethods.Post);
        this
            .Given(x => x.GivenThereIsAnEchoServiceRunningOn(port))
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
    [Trait("Feat", "2058")] // https://github.com/ThreeMammals/Ocelot/pull/2058
    [Trait("Bug", "2059")] // https://github.com/ThreeMammals/Ocelot/issues/2059
    public void Should_return_same_cached_response_when_request_body_changes_and_EnableContentHashing_is_false(bool asGlobalConfig)
    {
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options, asGlobalConfig, HttpMethods.Post);
        this
            .Given(x => x.GivenThereIsAnEchoServiceRunningOn(port))
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

    [Theory]
    [InlineData(null, HttpStatusCode.OK)]
    [InlineData(null, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.InternalServerError)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(new int[] { (int)HttpStatusCode.OK, (int)HttpStatusCode.Forbidden }, HttpStatusCode.OK)]
    [InlineData(new int[] { StatusCodes.Status200OK, StatusCodes.Status403Forbidden }, HttpStatusCode.Forbidden)]
    [Trait("Feat", "741")]
    public void Should_cache_when_whitelisted(int[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            StatusCodes = statusCodes,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, responseCode, HelloLauraContent, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(responseCode))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, responseCode, HelloTomContent, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(responseCode))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .And(x => ThenTheContentLengthIs(HelloLauraContent.Length))
            .BDDfy();
    }

    [Theory]
    [InlineData(new int[] { StatusCodes.Status200OK, StatusCodes.Status403Forbidden }, HttpStatusCode.InternalServerError)]
    [InlineData(new int[] { StatusCodes.Status200OK, StatusCodes.Status403Forbidden }, HttpStatusCode.BadRequest)]
    [Trait("Feat", "741")]
    public void Should_not_cache_when_not_whitelisted(int[] statusCodes, HttpStatusCode responseCode)
    {
        // Arrange
        var port = PortFinder.GetRandomPort();
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            StatusCodes = statusCodes,
        };
        var (testBody1String, testBody2String) = TestBodiesFactory();
        var configuration = GivenFileConfiguration(port, options);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, responseCode, HelloLauraContent, null, null))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(responseCode))
            .And(x => ThenTheResponseBodyShouldBe(HelloLauraContent))
            .Given(x => x.GivenTheServiceNowReturns(port, responseCode, HelloTomContent, null, null))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(responseCode))
            .And(x => ThenTheResponseBodyShouldBe(HelloTomContent))
            .And(x => ThenTheContentLengthIs(HelloTomContent.Length))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "1172")] // https://github.com/ThreeMammals/Ocelot/pull/1172
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
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, HelloFromLaura, headerExpires, options.TtlSeconds))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))

            // Read from cache
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, headerExpires, options.TtlSeconds / 2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromLaura))
            .And(x => ThenTheContentLengthIs(HelloFromLaura.Length))

            // Clean cache by the header and cache new content
            .Given(x => x.GivenTheServiceNowReturns(port, HttpStatusCode.OK, HelloFromTom, headerExpires, -1))
            .And(x => GivenIAddAHeader(options.Header, "123"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(HelloFromTom))
            .And(x => ThenTheContentLengthIs(HelloFromTom.Length))
        .BDDfy();
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

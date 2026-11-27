using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Authentication.ApiKey;
using Ocelot.DependencyInjection;

namespace Ocelot.Acceptance.Authentication;

public class ApiKeyTests : AuthSteps
{
    private string _apiServerRootUrl;
    private string _apiKeyValidationPath = "validateapikey";

    private readonly Action<ApiKeyAuthenticationOptions> _getOptions;
    private readonly Action<ApiKeyAuthenticationOptions> _postOptions;
    private Action<ApiKeyAuthenticationOptions> _options;
    private string _authProviderKey;

    public ApiKeyTests()
    {
        var mockValidationApiPort = PortFinder.GetRandomPort();
        _apiServerRootUrl = $"http://localhost:{mockValidationApiPort}";

        _getOptions = o =>
        {
            o.Authority = $"{_apiServerRootUrl}/{_apiKeyValidationPath}";
            o.Method = HttpMethod.Get;
        };

        _postOptions = o =>
        {
            o.Authority = $"{_apiServerRootUrl}/{_apiKeyValidationPath}";
            o.Method = HttpMethod.Post;
        };
    }

    protected static readonly string[] AcceptedKeys = ["testing"];
    protected static readonly string[] NoRoles = [];

    [Fact]
    public void Should_return_401_using_api_key()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post );
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, NoRoles))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [Fact]
    public void Should_return_201_using_api_key()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, NoRoles))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    [Fact]
    public void Should_return_401_using_post()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, NoRoles))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_postOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [Fact]
    public void should_return_201_using_api_key_with_post()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, NoRoles))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_postOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    [Fact]
    public void Should_return_403_when_user_has_incorrect_role()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post);
        route.RouteClaimsRequirement = new()
        {
            { "Role", "testing" }
        };
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, NoRoles))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
        .BDDfy();
    }

    [Fact]
    public void Should_return_201_when_user_has_correct_role()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "TestApiKey", method: HttpMethods.Post);
        route.RouteClaimsRequirement = new()
        {
            { "Role", "testing" }
        };
        var configuration = GivenConfiguration(route);
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, AcceptedKeys, new[] { "testing" }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    private async Task GivenThereIsAMockKeyValidationApiServerOn(string url, string validationPath, string[] acceptedKeys, string[] roles)
    {
        var testBody = new
        {
            Owner = "testuser",
            Roles = roles
        };

        IWebHost host = new WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("OK");
                    });

                    endpoints.MapGet($"/{validationPath}", async context =>
                    {
                        if (!context.Request.Query.TryGetValue("key", out var key))
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("");
                        }

                        if (acceptedKeys.Any(x => x == key))
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(testBody));
                        }
                        else
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("");
                        }
                    });

                    endpoints.MapPost($"/{validationPath}", async context =>
                    {
                        using var r = new StreamReader(context.Request.Body);
                        var streamBody = await r.ReadToEndAsync();

                        var body = JsonConvert.DeserializeObject<PostBody>(streamBody);
                        var key = body.Key;
                        if (acceptedKeys.Any(x => x == key))
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(testBody));
                        }
                        else
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("");
                        }
                    });
                });
            })
            .Build();

        await host.StartAsync();
        await VerifyServerStarted(_apiServerRootUrl);
    }

    private static async Task VerifyServerStarted(string url)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
    }

    private void WithAuthApiKey(IServiceCollection services)
    {
        services.AddOcelot();
        services.AddAuthentication().AddApiKey(_authProviderKey, _options);
    }

    public int GivenOcelotIsRunning(Action<ApiKeyAuthenticationOptions> options, string authenticationProviderKey)
    {
        _options = options;
        _authProviderKey = authenticationProviderKey;
        return GivenOcelotIsRunning(WithAuthApiKey);
    }
}

public class PostBody
{
    public string Key { get; set; }
}

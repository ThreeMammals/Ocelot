using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ocelot.Authentication.ApiKey;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests.Authentication;

public class ApiKeyTests : AuthSteps
{
    private string _apiServerRootUrl;
    private string _downstreamServicePath = "/";
    private string _downstreamServiceHost = "localhost";
    private string _downstreamServiceScheme = "http";
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

    [Fact]
    public void should_return_401_using_api_key()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey"
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [Fact]
    public void should_return_201_using_api_key()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey",
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    [Fact]
    public void should_return_401_using_post()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey"
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { }))
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

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey"
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_postOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    [Fact]
    public void should_return_403_when_user_has_incorrect_role()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    RouteClaimsRequirement = new Dictionary<string, string>
                    {
                        { "Role", "testing" }
                    },
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey",
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
        .BDDfy();
    }

    [Fact]
    public void should_return_201_when_user_has_correct_role()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new FileRoute
                {
                    DownstreamPathTemplate = _downstreamServicePath,
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = _downstreamServiceHost,
                            Port = port
                        }
                    },
                    DownstreamScheme = _downstreamServiceScheme,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Post"],
                    RouteClaimsRequirement = new Dictionary<string, string>
                    {
                        { "Role", "testing" }
                    },
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "TestApiKey",
                    }
                }
            }
        };
        var body = Body();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => x.GivenThereIsAMockKeyValidationApiServerOn(_apiServerRootUrl, _apiKeyValidationPath, new string[] { "testing" }, new string[] { "testing" }))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(_getOptions, "TestApiKey"))
            .When(x => WhenIPostUrlOnTheApiGateway("/?key=testing", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    private void GivenThereIsAMockKeyValidationApiServerOn(string url, string validationPath, string[] acceptedKeys, string[] roles)
    {
        var testBody = new
        {
            Owner = "testuser",
            Roles = roles
        };

        IWebHost test = new WebHostBuilder()
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
                        StreamReader r = new StreamReader(context.Request.Body);
                        var streamBody = await r.ReadToEndAsync();

                        var body = JsonConvert.DeserializeObject<PostBody>(streamBody);
                        var key = body.key;

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

        test.Start();
        VerifyServerStarted(_apiServerRootUrl);
    }

    public void VerifyServerStarted(string url)
    {
        using (var httpClient = new HttpClient())
        {
            var response = httpClient.GetAsync(url).GetAwaiter().GetResult();
            var content = response.Content.ReadAsStringAsync().GetAwaiter();
            response.EnsureSuccessStatusCode();
        }
    }

    public void WithAuthApiKey(IServiceCollection services)
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
    public string key { get; set; }
}

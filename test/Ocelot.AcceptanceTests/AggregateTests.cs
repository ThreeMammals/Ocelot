//using IdentityServer4.AccessTokenValidation;
//using IdentityServer4.Extensions;
//using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using System.Text;

namespace Ocelot.AcceptanceTests;

public sealed class AggregateTests : Steps
{
    private readonly string[] _downstreamPaths;

    public AggregateTests()
    {
        _downstreamPaths = new string[3];
    }

    [Fact]
    [Trait("Issue", "597")]
    public void Should_fix_issue_597()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/api/values?MailId={userid}",
                    UpstreamPathTemplate = "/key1data/{userid}",
                    UpstreamHttpMethod = ["Get"],
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    Key = "key1",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/api/values?MailId={userid}",
                    UpstreamPathTemplate = "/key2data/{userid}",
                    UpstreamHttpMethod = ["Get"],
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    Key = "key2",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/api/values?MailId={userid}",
                    UpstreamPathTemplate = "/key3data/{userid}",
                    UpstreamHttpMethod = ["Get"],
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    Key = "key3",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/api/values?MailId={userid}",
                    UpstreamPathTemplate = "/key4data/{userid}",
                    UpstreamHttpMethod = ["Get"],
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    Key = "key4",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    RouteKeys = ["key1", "key2", "key3", "key4"],
                    UpstreamPathTemplate = "/EmpDetail/IN/{userid}",
                },
                new FileAggregateRoute
                {
                    RouteKeys = ["key1", "key2"],
                    UpstreamPathTemplate = "/EmpDetail/US/{userid}",
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = "CorrelationID",
            },
        };

        var expected = "{\"key1\":some_data,\"key2\":some_data}";
        this.Given(x => x.GivenServiceIsRunning(port, HttpStatusCode.OK, "some_data"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/EmpDetail/US/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_advanced_aggregate_configs()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var port3 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/Comments",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Comments",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/users/{userId}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/UserDetails/{userId}",
                    UpstreamHttpMethod = ["Get"],
                    Key = "UserDetails",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/posts/{postId}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port3,
                        },
                    },
                    UpstreamPathTemplate = "/PostDetails/{postId}",
                    UpstreamHttpMethod = ["Get"],
                    Key = "PostDetails",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = ["Comments", "UserDetails", "PostDetails"],
                    RouteKeysConfig = new()
                    {
                        new AggregateRouteConfig
                            { RouteKey = "UserDetails", JsonPath = "$[*].writerId", Parameter = "userId" },
                        new AggregateRouteConfig
                            { RouteKey = "PostDetails", JsonPath = "$[*].postId", Parameter = "postId" },
                    },
                },
            },
        };

        var userDetailsResponseContent = @"{""id"":1,""firstName"":""abolfazl"",""lastName"":""rajabpour""}";
        var postDetailsResponseContent = @"{""id"":1,""title"":""post1""}";
        var commentsResponseContent = @"[{""id"":1,""writerId"":1,""postId"":2,""text"":""text1""},{""id"":2,""writerId"":1,""postId"":2,""text"":""text2""}]";

        var expected = "{\"Comments\":" + commentsResponseContent + ",\"UserDetails\":" + userDetailsResponseContent + ",\"PostDetails\":" + postDetailsResponseContent + "}";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, commentsResponseContent))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/users/1", 200, userDetailsResponseContent))
            .Given(x => x.GivenServiceIsRunning(2, port3, "/posts/2", 200, postDetailsResponseContent))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_user_defined_aggregate()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                },

                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/tom",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Tom",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = ["Laura", "Tom"],
                    Aggregator = "FakeDefinedAggregator",
                },
            },
        };

        var expected = "Bye from Laura, Bye from Tom";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithSpecificAggregatorsRegisteredInDi<FakeDefinedAggregator, FakeDep>())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/laura", "Laura");
        var route2 = GivenRoute(port2, "/tom", "Tom");
        var configuration = GivenConfiguration(route1, route2);

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}"))
            .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_one_service_404()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/tom",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Tom",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = ["Laura", "Tom"],
                },
            },
        };

        var expected = "{\"Laura\":,\"Tom\":{Hello from Tom}}";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 404, ""))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_both_service_404()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/tom",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Tom",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = ["Laura", "Tom"],
                },
            },
        };

        var expected = "{\"Laura\":,\"Tom\":}";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 404, ""))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/", 404, ""))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
            .BDDfy();
    }

    [Fact]
    public void Should_be_thread_safe()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new()
            {
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                },
                new FileRoute
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/tom",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Tom",
                },
            },
            Aggregates = new()
            {
                new FileAggregateRoute
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = ["Laura", "Tom"],
                },
            },
        };

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIMakeLotsOfDifferentRequestsToTheApiGateway())
            .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
            .BDDfy();
    }

    private void WhenIMakeLotsOfDifferentRequestsToTheApiGateway()
    {
        var numberOfRequests = 100;
        var aggregateUrl = "/";
        var aggregateExpected = "{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}";
        var tomUrl = "/tom";
        var tomExpected = "{Hello from Tom}";
        var lauraUrl = "/laura";
        var lauraExpected = "{Hello from Laura}";

        var aggregateTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            aggregateTasks[i] = Fire(aggregateUrl, aggregateExpected, random);
        }

        var tomTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            tomTasks[i] = Fire(tomUrl, tomExpected, random);
        }

        var lauraTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            lauraTasks[i] = Fire(lauraUrl, lauraExpected, random);
        }

        Task.WaitAll(lauraTasks);
        Task.WaitAll(tomTasks);
        Task.WaitAll(aggregateTasks);
    }

    private async Task Fire(string url, string expectedBody, Random random)
    {
        var request = new HttpRequestMessage(new HttpMethod("GET"), url);
        await Task.Delay(random.Next(0, 2));
        var response = await ocelotClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(expectedBody);
    }

    //[Fact]
    //[Trait("Bug", "1396")]
    //public void Should_return_response_200_with_user_forwarding()
    //{
    //    var port1 = PortFinder.GetRandomPort();
    //    var port2 = PortFinder.GetRandomPort();
    //    var port3 = PortFinder.GetRandomPort();
    //    var route1 = GivenRouteWithKey(port1, "/laura", "Laura");
    //    var route2 = GivenRouteWithKey(port2, "/tom", "Tom");
    //    var configuration = GivenConfiguration(route1, route2);
    //    var identityServerUrl = $"{Uri.UriSchemeHttp}://localhost:{port3}";
    //    void configureOptions(IdentityServerAuthenticationOptions o)
    //    {
    //        o.Authority = identityServerUrl;
    //        o.ApiName = "api";
    //        o.RequireHttpsMetadata = false;
    //        o.SupportedTokens = SupportedTokens.Both;
    //        o.ApiSecret = "secret";
    //        o.ForwardDefault = IdentityServerAuthenticationDefaults.AuthenticationScheme;
    //    }
    //    Action<IServiceCollection> configureServices = s =>
    //    {
    //        s.AddOcelot();
    //        s.AddMvcCore(mvc =>
    //        {
    //            var policy = new AuthorizationPolicyBuilder()
    //                .RequireAuthenticatedUser()
    //                .RequireClaim("scope", "api")
    //                .Build();
    //            mvc.Filters.Add(new AuthorizeFilter(policy));
    //        });
    //        s.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
    //            .AddIdentityServerAuthentication(configureOptions);
    //    };
    //    var count = 0;
    //    var actualContexts = new HttpContext[2];
    //    Action<IApplicationBuilder> configureApp = async (app) =>
    //    {
    //        var configuration = new OcelotPipelineConfiguration
    //        {
    //            PreErrorResponderMiddleware = async (context, next) =>
    //            {
    //                var auth = await context.AuthenticateAsync();
    //                context.User = (auth.Succeeded && auth.Principal?.IsAuthenticated() == true)
    //                    ? auth.Principal : null;
    //                await next.Invoke();
    //            },
    //            AuthorizationMiddleware = (context, next) =>
    //            {
    //                actualContexts[count++] = context;
    //                return next.Invoke();
    //            },
    //        };
    //        await app.UseOcelot(configuration);
    //    };
    //    using (var auth = new AuthenticationTests())
    //    {
    //        this.Given(x => auth.GivenThereIsAnIdentityServerOn(identityServerUrl, AccessTokenType.Jwt))
    //            .And(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
    //            .And(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
    //            .And(x => auth.GivenToken(identityServerUrl))
    //            .And(x => auth.GivenThereIsAConfiguration(configuration))
    //            .And(x => auth.GivenOcelotIsRunning(configureServices, configureApp))
    //            .And(x => auth.GivenIHaveAddedATokenToMyRequest())
    //            .When(x => auth.WhenIGetUrlOnTheApiGatewayWithRequestId("/"))
    //            .Then(x => auth.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
    //            .And(x => auth.ThenTheResponseBodyShouldBe("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}"))
    //            .And(x => x.ThenTheDownstreamUrlPathShouldBe("/", "/"))
    //            .BDDfy();
    //    }

    //    // Assert
    //    for (var i = 0; i < actualContexts.Length; i++)
    //    {
    //        var ctx = actualContexts[i].ShouldNotBeNull();
    //        ctx.Items.DownstreamRoute().Key.ShouldBe(configuration.Routes[i].Key);
    //        var user = ctx.User.ShouldNotBeNull();
    //        user.IsAuthenticated().ShouldBeTrue();
    //        user.Claims.Count().ShouldBeGreaterThan(1);
    //        user.Claims.FirstOrDefault(c => c is { Type: "scope", Value: "api" }).ShouldNotBeNull();
    //    }
    //}
    [Fact]
    [Trait("Bug", "2039")]
    public void Should_return_response_200_with_copied_body_sent_on_multiple_services()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/Service1", "Service1", "/Sub1");
        var route2 = GivenRoute(port2, "/Service2", "Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);
        var requestBody = @"{""id"":1,""response"":""fromBody-#REPLACESTRING#""}";
        var sub1ResponseContent = @"{""id"":1,""response"":""fromBody-s1""}";
        var sub2ResponseContent = @"{""id"":1,""response"":""fromBody-s2""}";
        var expected = $"{{\"Service1\":{sub1ResponseContent},\"Service2\":{sub2ResponseContent}}}";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/Sub1", 200, reqBody => reqBody.Replace("#REPLACESTRING#", "s1")))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/Sub2", 200, reqBody => reqBody.Replace("#REPLACESTRING#", "s2")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithBody("/", requestBody))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2039")]
    public void Should_return_response_200_with_copied_form_sent_on_multiple_services()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/Service1", "Service1", "/Sub1");
        var route2 = GivenRoute(port2, "/Service2", "Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);

        var formValues = new[]
        {
            new KeyValuePair<string, string>("param1", "value1"),
            new KeyValuePair<string, string>("param2", "from-form-REPLACESTRING"),
        };

        var sub1ResponseContent = "\"[key:param1=value1&param2=from-form-s1]\"";
        var sub2ResponseContent = "\"[key:param1=value1&param2=from-form-s2]\"";
        var expected = $"{{\"Service1\":{sub1ResponseContent},\"Service2\":{sub2ResponseContent}}}";

        this.Given(x => x.GivenServiceIsRunning(0, port1, "/Sub1", 200, (IFormCollection reqForm) => FormatFormCollection(reqForm).Replace("REPLACESTRING", "s1")))
            .Given(x => x.GivenServiceIsRunning(1, port2, "/Sub2", 200, (IFormCollection reqForm) => FormatFormCollection(reqForm).Replace("REPLACESTRING", "s2")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithForm("/", "key", formValues))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2248")]
    [Trait("PR", "2328")] // https://github.com/ThreeMammals/Ocelot/pull/2328
    public void Should_match_downstream_routes_using_route_keys_array()
    {
        var portUser = PortFinder.GetRandomPort();
        var userRoute = GivenRoute(portUser, "/user", "/user");
        userRoute.Key = "User";

        var portProduct = PortFinder.GetRandomPort();
        var productRoute = GivenRoute(portProduct, "/product", "/product");
        productRoute.Key = "Product";

        var aggregate = new FileAggregateRoute
        {
            RouteKeys = new() { "User", "Product" },
            UpstreamPathTemplate = "/composite",
            UpstreamHttpMethod = ["Get"],
        };

        var configuration = GivenConfiguration(userRoute, productRoute);
        configuration.Aggregates = new() { aggregate };
        this.Given(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenThereIsAServiceRunningOn(portUser, "/user", MapGetUser))
            .And(_ => GivenThereIsAServiceRunningOn(portProduct, "/product", MapGetProduct))
            .When(_ => WhenIGetUrlOnTheApiGateway("/composite"))
            .Then(_ => ThenTheStatusCodeShouldBeOK())
            .BDDfy();
    }

    Task MapGetUser(HttpContext ctx)
    {
        ctx.Response.StatusCode = 200;
        return ctx.Response.WriteAsync("OK-user");
    }
    Task MapGetProduct(HttpContext ctx)
    {
        ctx.Response.StatusCode = 200;
        return ctx.Response.WriteAsync("OK-product");
    }

    private static string FormatFormCollection(IFormCollection reqForm)
    {
        var sb = new StringBuilder()
            .Append('"');

        foreach (var kvp in reqForm)
        {
            sb.Append($"[{kvp.Key}:{kvp.Value}]");
        }

        return sb
            .Append('"')
            .ToString();
    }

    private void GivenServiceIsRunning(int port, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        });
    }

    private void GivenServiceIsRunning(int index, int port, string basePath, int statusCode, string responseBody)
        => GivenServiceIsRunning(index, port, basePath, statusCode,
            async context =>
            {
                await context.Response.WriteAsync(responseBody);
            });

    private void GivenServiceIsRunning(int index, int port, string basePath, int statusCode, Func<string, string> responseFromBody)
        => GivenServiceIsRunning(index, port, basePath, statusCode,
            async context =>
            {
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var responseBody = responseFromBody(requestBody);
                await context.Response.WriteAsync(responseBody);
            });

    private void GivenServiceIsRunning(int index, int port, string basePath, int statusCode, Func<IFormCollection, string> responseFromForm)
        => GivenServiceIsRunning(index, port, basePath, statusCode,
            async context =>
            {
                var responseBody = responseFromForm(context.Request.Form);
                await context.Response.WriteAsync(responseBody);
            });

    private void GivenServiceIsRunning(int index, int port, string basePath, int statusCode, Action<HttpContext> processContext)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            _downstreamPaths[index] = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value
                : context.Request.Path.Value;

            if (_downstreamPaths[index] != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("downstream path doesn't match base path");
            }
            else
            {
                context.Response.StatusCode = statusCode;
                processContext?.Invoke(context);
            }
        });
    }

    private void GivenOcelotIsRunningWithSpecificAggregatorsRegisteredInDi<TAggregator, TDependency>()
        where TAggregator : class, IDefinedAggregator
        where TDependency : class
    {
        static void WithSpecificAggregators(IServiceCollection services) => services
            .AddSingleton<TDependency>()
            .AddOcelot()
            .AddSingletonDefinedAggregator<TAggregator>();
        GivenOcelotIsRunning(WithSpecificAggregators);
    }

    private void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPathOne, string expectedDownstreamPath)
    {
        _downstreamPaths[0].ShouldBe(expectedDownstreamPathOne);
        _downstreamPaths[1].ShouldBe(expectedDownstreamPath);
    }

    private static FileRoute GivenRoute(int port, string upstream, string key, string downstream = null) => new()
    {
        DownstreamPathTemplate = downstream ?? "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = new() { new("localhost", port) },
        UpstreamPathTemplate = upstream,
        UpstreamHttpMethod = [HttpMethods.Get],
        Key = key,
    };

    private FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var obj = base.GivenConfiguration(routes);
        obj.Aggregates.Add(
            new()
            {
                UpstreamPathTemplate = "/",
                UpstreamHost = "localhost",
                RouteKeys = new(routes.Select(r => r.Key)), // [ "Laura", "Tom" ],
            }
        );
        return obj;
    }
}

public class FakeDep
{
}

public class FakeDefinedAggregator : IDefinedAggregator
{
    public FakeDefinedAggregator(FakeDep dep)
    {
    }

    public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
    {
        var one = await responses[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
        var two = await responses[1].Items.DownstreamResponse().Content.ReadAsStringAsync();

        var merge = $"{one}, {two}";
        merge = merge.Replace("Hello", "Bye").Replace("{", "").Replace("}", "");
        var headers = responses.SelectMany(x => x.Items.DownstreamResponse().Headers).ToList();
        return new DownstreamResponse(new StringContent(merge), HttpStatusCode.OK, headers, "some reason");
    }
}

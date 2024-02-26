using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace Ocelot.AcceptanceTests
{
    public class AggregateTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;
        private readonly string[] _downstreamPaths;

        public AggregateTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
            _downstreamPaths = new string[3];
        }

        [Fact]
        [Trait("Issue", "597")]
        public void should_fix_issue_597()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/values?MailId={userid}",
                        UpstreamPathTemplate = "/key1data/{userid}",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        Key = "key1",
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/values?MailId={userid}",
                        UpstreamPathTemplate = "/key2data/{userid}",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        Key = "key2",
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/values?MailId={userid}",
                        UpstreamPathTemplate = "/key3data/{userid}",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        Key = "key3",
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/values?MailId={userid}",
                        UpstreamPathTemplate = "/key4data/{userid}",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        Key = "key4",
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new()
                    {
                        RouteKeys = new List<string>{
                            "key1",
                            "key2",
                            "key3",
                            "key4",
                        },
                        UpstreamPathTemplate = "/EmpDetail/IN/{userid}",
                    },
                    new()
                    {
                        RouteKeys = new List<string>{
                            "key1",
                            "key2",
                        },
                        UpstreamPathTemplate = "/EmpDetail/US/{userid}",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "CorrelationID",
                },
            };

            var expected = "{\"key1\":some_data,\"key2\":some_data}";

            this.Given(x => x.GivenServiceIsRunning($"http://localhost:{port}", 200, "some_data"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EmpDetail/US/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_advanced_aggregate_configs()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var port3 = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port1,
                                },
                            },
                            UpstreamPathTemplate = "/Comments",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Comments",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/users/{userId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port2,
                                },
                            },
                            UpstreamPathTemplate = "/UserDetails",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "UserDetails",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/posts/{postId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port3,
                                },
                            },
                            UpstreamPathTemplate = "/PostDetails",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "PostDetails",
                        },
                    },
                Aggregates = new List<FileAggregateRoute>
                    {
                        new()
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            RouteKeys = new List<string>
                            {
                                "Comments",
                                "UserDetails",
                                "PostDetails",
                            },
                            RouteKeysConfig = new List<AggregateRouteConfig>()
                            {
                                new(){RouteKey = "UserDetails",JsonPath = "$[*].writerId",Parameter = "userId"},
                                new(){RouteKey = "PostDetails",JsonPath = "$[*].postId",Parameter = "postId"},
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
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_user_defined_aggregate()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port1,
                                },
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port2,
                                },
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom",
                        },
                    },
                Aggregates = new List<FileAggregateRoute>
                    {
                        new()
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            RouteKeys = new List<string>
                            {
                                "Laura",
                                "Tom",
                            },
                            Aggregator = "FakeDefinedAggregator",
                        },
                    },
            };

            var expected = "Bye from Laura, Bye from Tom";

            this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithSpecificAggregatorsRegisteredInDi<FakeDefinedAggregator, FakeDepdendency>())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var route1 = GivenRoute(port1, "/laura", "Laura");
            var route2 = GivenRoute(port2, "/tom", "Tom");
            var configuration = GivenConfiguration(route1, route2);

            this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}"))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_one_service_404()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port1,
                                },
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port2,
                                },
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom",
                        },
                    },
                Aggregates = new List<FileAggregateRoute>
                    {
                        new()
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            RouteKeys = new List<string>
                            {
                                "Laura",
                                "Tom",
                            },
                        },
                    },
            };

            var expected = "{\"Laura\":,\"Tom\":{Hello from Tom}}";

            this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 404, ""))
                .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_both_service_404()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port1,
                                },
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port2,
                                },
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom",
                        },
                    },
                Aggregates = new List<FileAggregateRoute>
                    {
                        new()
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            RouteKeys = new List<string>
                            {
                                "Laura",
                                "Tom",
                            },
                        },
                    },
            };

            var expected = "{\"Laura\":,\"Tom\":}";

            this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 404, ""))
                .Given(x => x.GivenServiceIsRunning(1, port2, "/", 404, ""))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_be_thread_safe()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port1,
                                },
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura",
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port2,
                                },
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom",
                        },
                    },
                Aggregates = new List<FileAggregateRoute>
                    {
                        new()
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            RouteKeys = new List<string>
                            {
                                "Laura",
                                "Tom",
                            },
                        },
                    },
            };

            this.Given(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIMakeLotsOfDifferentRequestsToTheApiGateway())
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        [Trait("Bug", "1396")]
        public void should_return_response_200_with_user_forwarding()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var port3 = PortFinder.GetRandomPort();
            var route1 = GivenRoute(port1, "/laura", "Laura");
            var route2 = GivenRoute(port2, "/tom", "Tom");
            var configuration = GivenConfiguration(route1, route2);
            var identityServerUrl = $"{Uri.UriSchemeHttp}://localhost:{port3}";
            Action<IdentityServerAuthenticationOptions> options = o =>
            {
                o.Authority = identityServerUrl;
                o.ApiName = "api";
                o.RequireHttpsMetadata = false;
                o.SupportedTokens = SupportedTokens.Both;
                o.ApiSecret = "secret";
                o.ForwardDefault = IdentityServerAuthenticationDefaults.AuthenticationScheme;
            };
            Action<IServiceCollection> configureServices = s =>
            {
                s.AddOcelot();
                s.AddMvcCore(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", "api")
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                });
                s.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                    .AddIdentityServerAuthentication(options);
            };
            var count = 0;
            var actualContexts = new HttpContext[2];
            Action<IApplicationBuilder> configureApp = async (app) =>
            {
                var configuration = new OcelotPipelineConfiguration
                {
                    PreErrorResponderMiddleware = async (context, next) =>
                    {
                        var auth = await context.AuthenticateAsync();
                        context.User = (auth.Succeeded && auth.Principal?.IsAuthenticated() == true)
                            ? auth.Principal : null;
                        await next.Invoke();
                    },
                    AuthorizationMiddleware = (context, next) =>
                    {
                        actualContexts[count++] = context;
                        return next.Invoke();
                    },
                };
                await app.UseOcelot(configuration);
            };
            using (var auth = new AuthenticationTests())
            {
                this.Given(x => auth.GivenThereIsAnIdentityServerOn(identityServerUrl, AccessTokenType.Jwt))
                    .And(x => x.GivenServiceIsRunning(0, port1, "/", 200, "{Hello from Laura}"))
                    .And(x => x.GivenServiceIsRunning(1, port2, "/", 200, "{Hello from Tom}"))
                    .And(x => auth.GivenIHaveAToken(identityServerUrl))
                    .And(x => auth.GivenThereIsAConfiguration(configuration))
                    .And(x => auth.GivenOcelotIsRunningWithServices(configureServices, configureApp))
                    .And(x => auth.GivenIHaveAddedATokenToMyRequest())
                    .When(x => auth.WhenIGetUrlOnTheApiGateway("/"))
                    .Then(x => auth.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                    .And(x => auth.ThenTheResponseBodyShouldBe("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}"))
                    .And(x => x.ThenTheDownstreamUrlPathShouldBe("/", "/"))
                    .BDDfy();
            }

            // Assert
            for (int i = 0; i < actualContexts.Length; i++)
            {
                var ctx = actualContexts[i].ShouldNotBeNull();
                ctx.Items.DownstreamRoute().Key.ShouldBe(configuration.Routes[i].Key);
                var user = ctx.User.ShouldNotBeNull();
                user.IsAuthenticated().ShouldBeTrue();
                user.Claims.Count().ShouldBeGreaterThan(1);
                user.Claims.FirstOrDefault(c => c.Type == "scope" && c.Value == "api").ShouldNotBeNull();
            }
        }

        private void GivenServiceIsRunning(string baseUrl, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        private void GivenServiceIsRunning(int index, int port, string basePath, int statusCode, string responseBody)
        {
            var baseUrl = $"{Uri.UriSchemeHttp}://localhost:{port}";
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPaths[index] = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                if (_downstreamPaths[index] != basePath)
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync("downstream path didnt match base path");
                }
                else
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                }
            });
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPathOne, string expectedDownstreamPath)
        {
            _downstreamPaths[0].ShouldBe(expectedDownstreamPathOne);
            _downstreamPaths[1].ShouldBe(expectedDownstreamPath);
        }

        private static FileRoute GivenRoute(int port, string upstream, string key) => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamScheme = Uri.UriSchemeHttp,
            DownstreamHostAndPorts = [new("localhost", port)],
            UpstreamPathTemplate = upstream,
            UpstreamHttpMethod = [HttpMethods.Get],
            Key = key,
        };

        private static FileConfiguration GivenConfiguration(params FileRoute[] routes) => new()
        {
            Routes = new(routes),
            Aggregates =
            [
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = routes.Select(r => r.Key).ToList(), // [ "Laura", "Tom" ],
                },
            ],
        };

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }
    }

    public class FakeDepdendency
    {
    }

    public class FakeDefinedAggregator : IDefinedAggregator
    {
        private readonly FakeDepdendency _dep;

        public FakeDefinedAggregator(FakeDepdendency dep)
        {
            _dep = dep;
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
}

using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    public sealed class RoutingTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;
        private string _downstreamPath;
        private string _downstreamQuery;

        public RoutingTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }

        [Fact]
        public void should_not_match_forward_slash_in_pattern_before_next_forward_slash()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/v{apiVersion}/cards",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/api/v{apiVersion}/cards",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            Priority = 1,
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/api/v1/aaaaaaaaa/cards", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/v1/aaaaaaaaa/cards"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_404_when_no_configuration_at_all()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(new FileConfiguration()))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_forward_slash_and_placeholder_only()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_favouring_forward_slash_with_path_route()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
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
                                    Port = 50810,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/test", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/test"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_favouring_forward_slash()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = 51880,
                                },
                            },
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
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
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_favouring_forward_slash_route_because_it_is_first()
        {
            var port = PortFinder.GetRandomPort();

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
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = 51879,
                                },
                            },
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_nothing_and_placeholder_only()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(string.Empty))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var port = PortFinder.GetRandomPort();

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
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        [Trait("Bug", "134")]
        public void should_fix_issue_134()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/v1/vacancy",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/vacancy/",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/v1/vacancy/{vacancyId}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/vacancy/{vacancyId}",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/v1/vacancy/1", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/vacancy/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_path_missing_forward_slash_as_first_char()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/products",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_host_has_trailing_slash()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/products",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Theory]
        [InlineData("/products")]
        [InlineData("/products/")]
        public void should_return_ok_when_upstream_url_ends_with_forward_slash_but_template_does_not(string url)
        {
            var port = PortFinder.GetRandomPort();
            var downstreamBasePath = "/products";
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = downstreamBasePath,
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = downstreamBasePath+"/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", downstreamBasePath, HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(url))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "649")]
        [InlineData("/account/authenticate")]
        [InlineData("/account/authenticate/")]
        public void should_fix_issue_649(string url)
        {
            var port = PortFinder.GetRandomPort();
            var baseUrl = $"http://localhost:{port}";
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        UpstreamPathTemplate = "/account/authenticate/",

                        DownstreamPathTemplate = "/authenticate",
                        DownstreamScheme = Uri.UriSchemeHttp,
                        DownstreamHostAndPorts = new()
                        {
                            new("localhost", port),
                        },
                    },
                },
                GlobalConfiguration =
                {
                    BaseUrl = baseUrl,
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(baseUrl, "/authenticate", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(url))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_not_found_when_upstream_url_ends_with_forward_slash_but_template_does_not()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/products",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/products",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/products", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Theory]
        [InlineData("/products/{productId}", "/products/{productId}", "/products/")]

        public void should_return_response_200_with_empty_placeholder(string downstreamPathTemplate, string upstreamPathTemplate, string requestURL)
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = downstreamPathTemplate,
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new("localhost", port),
                        },
                        UpstreamPathTemplate = upstreamPathTemplate,
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", requestURL, HttpStatusCode.OK, "Hello from Aly"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(requestURL))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Aly"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_complex_url()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", HttpStatusCode.OK, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_complex_url_that_starts_with_placeholder()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/{variantId}/products/{productId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/{variantId}/products/{productId}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/23/products/1", HttpStatusCode.OK, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("23/products/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Fact]
        public void should_not_add_trailing_slash_to_downstream_url()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", HttpStatusCode.OK, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/1"))
                .Then(x => ThenTheDownstreamUrlPathShouldBe("/api/products/1"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Post" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.Created, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_complex_query_string()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/newThing",
                            UpstreamPathTemplate = "/newThing",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/newThing", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/newThing?DeviceType=IphoneApp&Browser=moonpigIphone&BrowserString=-&CountryCode=123&DeviceName=iPhone 5 (GSM+CDMA)&OperatingSystem=iPhone OS 7.1.2&BrowserVersion=3708AdHoc&ipAddress=-"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_placeholder_for_final_url_path()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/{urlPath}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/myApp1Name/api/{urlPath}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", HttpStatusCode.OK, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/myApp1Name/api/products/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "748")]
        [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test/1", "/downstream/test/1", "?p1=v1&p2=v2&something-else")]
        [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test/", "/downstream/test/", "?p1=v1&p2=v2&something-else")]
        [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test", "/downstream/test", "?p1=v1&p2=v2&something-else")]
        [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test123", null, null)]
        [InlineData("/downstream/{version}/test/{everything}", "/upstream/{version}/test/{everything}", "/upstream/v1/test/123", "/downstream/v1/test/123", "?p1=v1&p2=v2&something-else")]
        [InlineData("/downstream/{version}/test", "/upstream/{version}/test", "/upstream/v1/test", "/downstream/v1/test", "?p1=v1&p2=v2&something-else")]
        [InlineData("/downstream/{version}/test", "/upstream/{version}/test", "/upstream/test", null, null)]
        public void should_return_correct_downstream_when_omitting_ending_placeholder(string downstreamPathTemplate, string upstreamPathTemplate, string requestURL, string downstreamURL, string queryString)
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenDefaultConfiguration(port, upstreamPathTemplate, downstreamPathTemplate);
            this.Given(x => GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.OK, "Hello from Aly"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(requestURL))
                .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamURL))

                // Now check the same URL but with query string
                // Catch-All placeholder should forward any path + query string combinations to the downstream service
                // More: https://ocelot.readthedocs.io/en/latest/features/routing.html#placeholders:~:text=This%20will%20forward%20any%20path%20%2B%20query%20string%20combinations%20to%20the%20downstream%20service%20after%20the%20path%20%2Fapi.
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(requestURL + queryString))
                .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamURL))
                .And(x => x.ThenTheDownstreamUrlQueryStringShouldBe(queryString))
                .BDDfy();
        }

        [Trait("PR", "1911")]
        [Trait("Link", "https://ocelot.readthedocs.io/en/latest/features/routing.html#catch-all-query-string")]
        [Theory(DisplayName = "Catch All Query String should be forwarded with all query string parameters with(out) last slash")]
        [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts", "/apipath/contracts", "")]
        [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?", "/apipath/contracts", "")]
        [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?p1=v1&p2=v2", "/apipath/contracts", "?p1=v1&p2=v2")]
        [InlineData("/apipath/contracts/?{everything}", "/contracts/?{everything}", "/contracts/?", "/apipath/contracts/", "")]
        [InlineData("/apipath/contracts/?{everything}", "/contracts/?{everything}", "/contracts/?p3=v3&p4=v4", "/apipath/contracts/", "?p3=v3&p4=v4")]
        [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?filter=(-something+123+else)", "/apipath/contracts", "?filter=(-something%20123%20else)")]
        public void Should_forward_Catch_All_query_string_when_last_slash(string downstream, string upstream, string requestURL, string downstreamPath, string queryString)
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenDefaultConfiguration(port, upstream, downstream);
            this.Given(x => GivenThereIsAServiceRunningOn($"http://localhost:{port}", downstreamPath, HttpStatusCode.OK, "Hello from Raman"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(requestURL))
                .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamPath)) // !
                .And(x => x.ThenTheDownstreamUrlQueryStringShouldBe(queryString)) // !!
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Raman"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url_and_multiple_upstream_http_method()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get", "Post" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.Created, nameof(HttpStatusCode.Created)))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .And(x => _steps.ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.Created)))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_and_any_upstream_http_method()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string>(),
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_404_when_calling_upstream_route_with_no_matching_downstream_re_route_github_issue_134()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/v1/vacancy",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/vacancy/",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/v1/vacancy/{vacancyId}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/vacancy/{vacancyId}",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/v1/vacancy/1", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("api/vacancy/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_not_set_trailing_slash_on_url_template()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/{url}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/platform/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/swagger/lib/backbone-min.js", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/platform/swagger/lib/backbone-min.js"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/api/swagger/lib/backbone-min.js"))
                .BDDfy();
        }

        [Fact]
        public void should_use_priority()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/goods/{url}",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/goods/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = 53879,
                                },
                            },
                            Priority = 0,
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/goods/delete",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/goods/delete",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/goods/delete", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/goods/delete"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_match_multiple_paths_with_catch_all()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{everything}",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/{everything}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/test/toot", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/test/toot"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_fix_issue_271()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/v1/{everything}",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/api/v1/{everything}",
                            UpstreamHttpMethod = new List<string> { "Get", "Put", "Post" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/connect/token",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/connect/token",
                            UpstreamHttpMethod = new List<string> { "Post" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = 5001,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/api/v1/modules/Test", HttpStatusCode.OK, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/v1/modules/Test"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, HttpStatusCode statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value + context.Request.Path.Value : context.Request.Path.Value;
                _downstreamQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

                if (_downstreamPath != basePath)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    await context.Response.WriteAsync("Downstream path didn't match base path");
                }
                else
                {
                    context.Response.StatusCode = (int)statusCode;
                    await context.Response.WriteAsync(responseBody);
                }
            });
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
        {
            _downstreamPath.ShouldBe(expectedDownstreamPath);
        }

        internal void ThenTheDownstreamUrlQueryStringShouldBe(string expectedQueryString)
        {
            _downstreamQuery.ShouldBe(expectedQueryString);
        }

        private FileConfiguration GivenDefaultConfiguration(int port, string upstream, string downstream) => new()
        {
            Routes = new()
            {
                new()
                {
                    DownstreamPathTemplate = downstream,
                    DownstreamScheme = Uri.UriSchemeHttp,
                    DownstreamHostAndPorts =
                    {
                        new("localhost", port),
                    },
                    UpstreamPathTemplate = upstream,
                    UpstreamHttpMethod = new() { HttpMethods.Get },
                },
            },
        };
    }
}

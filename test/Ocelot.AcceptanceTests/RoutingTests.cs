namespace Ocelot.AcceptanceTests
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration.File;
    using Shouldly;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using TestStack.BDDfy;
    using Xunit;

    public class RoutingTests : IDisposable
    {
        private readonly Steps _steps;
        private string _downstreamPath;
        private readonly ServiceHandler _serviceHandler;

        public RoutingTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void should_not_match_forward_slash_in_pattern_before_next_forward_slash()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v{apiVersion}/cards",
                        UpstreamPathTemplate = "/api/v{apiVersion}/cards",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        Priority = 1,
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/api/v1/aaaaaaaaa/cards", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {                        
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/{url}",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            const string ClusterIdTwo = "cluster2";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/{url}",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                    new FileRoute
                    {
                        ClusterId = ClusterIdTwo,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                    {ClusterIdTwo, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{ClusterIdTwo}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:50810",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/test", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            const string ClusterIdTwo = "cluster2";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = ClusterIdTwo,
                        DownstreamPathTemplate = "/{url}",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                    {ClusterIdTwo, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{ClusterIdTwo}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            const string ClusterIdTwo = "cluster2";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },  
                    new FileRoute
                    {
                        ClusterId = ClusterIdTwo,
                        DownstreamPathTemplate = "/{url}",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                    {ClusterIdTwo, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{ClusterIdTwo}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:51879",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/{url}",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(""))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new FileRoute
                        {
                            ClusterId = _steps.ClusterOneId,
                            DownstreamPathTemplate = "/",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
                Clusters = new Dictionary<string, FileCluster>
                { 
                    {_steps.ClusterOneId, new FileCluster 
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        } 
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void bug()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v1/vacancy",
                        UpstreamPathTemplate = "/vacancy/",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                    new FileRoute
                    {                        
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v1/vacancy/{vacancyId}",
                        UpstreamPathTemplate = "/vacancy/{vacancyId}",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/v1/vacancy/1", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/products",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/products",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_ok_when_upstream_url_ends_with_forward_slash_but_template_does_not()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/products",
                        UpstreamPathTemplate = "/products/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/products", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_not_found_when_upstream_url_ends_with_forward_slash_but_template_does_not()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/products",
                        UpstreamPathTemplate = "/products",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/products", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_not_found()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/products",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/products", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_complex_url()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/products/{productId}",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", 200, "Some Product"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/{variantId}/products/{productId}",
                        UpstreamPathTemplate = "/{variantId}/products/{productId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/23/products/1", 200, "Some Product"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/products/{productId}",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/1"))
                .Then(x => ThenTheDownstreamUrlPathShouldBe("/api/products/1"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Post" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 201, string.Empty))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/newThing",
                        UpstreamPathTemplate = "/newThing",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/newThing", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/{urlPath}",
                        UpstreamPathTemplate = "/myApp1Name/api/{urlPath}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/myApp1Name/api/products/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url_and_multiple_upstream_http_method()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get", "Post" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "", 201, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_and_any_upstream_http_method()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string>(),
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v1/vacancy",
                        UpstreamPathTemplate = "/vacancy/",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v1/vacancy/{vacancyId}",
                        UpstreamPathTemplate = "/vacancy/{vacancyId}",
                        UpstreamHttpMethod = new List<string> { "Options",  "Put", "Get", "Post", "Delete" },
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/v1/vacancy/1", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("api/vacancy/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_not_set_trailing_slash_on_url_template()
        {
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/{url}",
                        UpstreamPathTemplate = "/platform/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/swagger/lib/backbone-min.js", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            const string ClusterTwoId = "cluster2";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = ClusterTwoId,
                        DownstreamPathTemplate = "/goods/{url}",
                        UpstreamPathTemplate = "/goods/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        Priority = 0,
                    },
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/goods/delete",
                        UpstreamPathTemplate = "/goods/delete",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                    {ClusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{ClusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{53879}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/goods/delete", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/{everything}",
                        UpstreamPathTemplate = "/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/test/toot", 200, "Hello from Laura"))
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
            var port = RandomPortFinder.GetRandomPort();

            const string ClusterTwoId = "cluster2";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/v1/{everything}",
                        UpstreamPathTemplate = "/api/v1/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get", "Put", "Post" },
                    },
                    new FileRoute
                    {
                        ClusterId = ClusterTwoId,
                        DownstreamPathTemplate = "/connect/token",
                        UpstreamPathTemplate = "/connect/token",
                        UpstreamHttpMethod = new List<string> { "Post" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{port}",
                                    }
                                },
                            },
                        }
                    },
                    {ClusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{ClusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{5001}",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/api/v1/modules/Test", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/v1/modules/Test"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                if (_downstreamPath != basePath)
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

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
        {
            _downstreamPath.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }
    }
}

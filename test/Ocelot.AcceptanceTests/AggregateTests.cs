using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class AggregateTests : IDisposable
    {
        private IWebHost _serviceOneBuilder;
        private IWebHost _serviceTwoBuilder;
        private readonly Steps _steps;
        private string _downstreamPathOne;
        private string _downstreamPathTwo;

        public AggregateTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_user_defined_aggregate()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51885,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51886,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                    Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string> 
                            {
                                "Tom",
                                "Laura"
                            },
                            Aggregator = "FakeDefinedAggregator"
                        }
                    }
            };

            var expected = "Bye from Laura, Bye from Tom";

            this.Given(x => x.GivenServiceOneIsRunning("http://localhost:51885", "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceTwoIsRunning("http://localhost:51886", "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithSpecficAggregatorsRegisteredInDi<FakeDefinedAggregator, FakeDepdendency>())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51885,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51886,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                    Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string> 
                            {
                                "Tom",
                                "Laura"
                            }
                        }
                    }
            };

            var expected = "{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}";

            this.Given(x => x.GivenServiceOneIsRunning("http://localhost:51885", "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceTwoIsRunning("http://localhost:51886", "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(expected))
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_one_service_404()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51881,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51882,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            }
                        }
                    }
            };

            var expected = "{\"Laura\":,\"Tom\":{Hello from Tom}}";

            this.Given(x => x.GivenServiceOneIsRunning("http://localhost:51881", "/", 404, ""))
                .Given(x => x.GivenServiceTwoIsRunning("http://localhost:51882", "/", 200, "{Hello from Tom}"))
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
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51883,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51884,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            }
                        }
                    }
            };

            var expected = "{\"Laura\":,\"Tom\":}";

            this.Given(x => x.GivenServiceOneIsRunning("http://localhost:51883", "/", 404, ""))
                .Given(x => x.GivenServiceTwoIsRunning("http://localhost:51884", "/", 404, ""))
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
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51878,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51880,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenServiceOneIsRunning("http://localhost:51878", "/", 200, "{Hello from Laura}"))
                .Given(x => x.GivenServiceTwoIsRunning("http://localhost:51880", "/", 200, "{Hello from Tom}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIMakeLotsOfDifferentRequestsToTheApiGateway())
                .And(x => ThenTheDownstreamUrlPathShouldBe("/", "/"))
                .BDDfy();
        }

        private void GivenServiceOneIsRunning(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceOneBuilder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        _downstreamPathOne = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                        if(_downstreamPathOne != basePath)
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
                })
                .Build();

            _serviceOneBuilder.Start();
        }

        private void GivenServiceTwoIsRunning(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceTwoBuilder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        _downstreamPathTwo = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                        if(_downstreamPathTwo != basePath)
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
                })
                .Build();

            _serviceTwoBuilder.Start();
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPathOne, string expectedDownstreamPath)
        {
            _downstreamPathOne.ShouldBe(expectedDownstreamPathOne);
            _downstreamPathTwo.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _serviceOneBuilder?.Dispose();
            _serviceTwoBuilder?.Dispose();
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

        public async Task<DownstreamResponse> Aggregate(List<DownstreamResponse> responses)
        {
            var one = await responses[0].Content.ReadAsStringAsync();
            var two = await responses[1].Content.ReadAsStringAsync();

            var merge = $"{one}, {two}";
            merge = merge.Replace("Hello", "Bye").Replace("{", "").Replace("}", "");
            var headers = responses.SelectMany(x => x.Headers).ToList();
            return new DownstreamResponse(new StringContent(merge), HttpStatusCode.OK, headers);
        }
    }
}

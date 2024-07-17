using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.AcceptanceTests
{
    public sealed class StickySessionsTests : Steps, IDisposable
    {
        private int _counterOne;
        private int _counterTwo;
        private static readonly object SyncLock = new();
        private readonly ServiceHandler _serviceHandler;

        public StickySessionsTests() : base()
        {
            _serviceHandler = new ServiceHandler();
        }

        public override void Dispose()
        {
            _serviceHandler?.Dispose();
            base.Dispose();
        }

        [Fact]
        public void Should_use_same_downstream_host()
        {
            var downstreamPortOne = PortFinder.GetRandomPort();
            var downstreamPortTwo = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamPortOne}";
            var downstreamServiceTwoUrl = $"http://localhost:{downstreamPortTwo}";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            LoadBalancerOptions = new FileLoadBalancerOptions
                            {
                                Type = nameof(CookieStickySessions),
                                Key = "sessionid",
                                Expiry = 300000,
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortOne,
                                },
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortTwo,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(_ => GivenThereIsAConfiguration(configuration))
                .And(_ => GivenOcelotIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10, "sessionid", "123"))
                .Then(x => x.ThenTheFirstServiceIsCalled(10))
                .Then(x => x.ThenTheSecondServiceIsCalled(0))
                .BDDfy();
        }

        [Fact]
        public void Should_use_different_downstream_host_for_different_routes()
        {
            var downstreamPortOne = PortFinder.GetRandomPort();
            var downstreamPortTwo = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamPortOne}";
            var downstreamServiceTwoUrl = $"http://localhost:{downstreamPortTwo}";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            LoadBalancerOptions = new FileLoadBalancerOptions
                            {
                                Type = nameof(CookieStickySessions),
                                Key = "sessionid",
                                Expiry = 300000,
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortOne,
                                },
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortTwo,
                                },
                            },
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/test",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            LoadBalancerOptions = new FileLoadBalancerOptions
                            {
                                Type = nameof(CookieStickySessions),
                                Key = "bestid",
                                Expiry = 300000,
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortTwo,
                                },
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortOne,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(_ => GivenThereIsAConfiguration(configuration))
                .And(_ => GivenOcelotIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGatewayWithCookie("/", "sessionid", "123"))
                .When(x => x.WhenIGetUrlOnTheApiGatewayWithCookie("/test", "bestid", "123"))
                .Then(x => x.ThenTheFirstServiceIsCalled(1))
                .Then(x => x.ThenTheSecondServiceIsCalled(1))
                .BDDfy();
        }

        [Fact]
        public void Should_use_same_downstream_host_for_different_routes()
        {
            var downstreamPortOne = PortFinder.GetRandomPort();
            var downstreamPortTwo = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamPortOne}";
            var downstreamServiceTwoUrl = $"http://localhost:{downstreamPortTwo}";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            LoadBalancerOptions = new FileLoadBalancerOptions
                            {
                                Type = nameof(CookieStickySessions),
                                Key = "sessionid",
                                Expiry = 300000,
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortOne,
                                },
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortTwo,
                                },
                            },
                        },
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/test",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            LoadBalancerOptions = new FileLoadBalancerOptions
                            {
                                Type = nameof(CookieStickySessions),
                                Key = "sessionid",
                                Expiry = 300000,
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortTwo,
                                },
                                new()
                                {
                                    Host = "localhost",
                                    Port = downstreamPortOne,
                                },
                            },
                        },
                    },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(_ => GivenThereIsAConfiguration(configuration))
                .And(_ => GivenOcelotIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGatewayWithCookie("/", "sessionid", "123"))
                .When(x => x.WhenIGetUrlOnTheApiGatewayWithCookie("/test", "sessionid", "123"))
                .Then(x => x.ThenTheFirstServiceIsCalled(2))
                .Then(x => x.ThenTheSecondServiceIsCalled(0))
                .BDDfy();
        }

        private Task<HttpResponseMessage> WhenIGetUrlOnTheApiGateway(string url, string cookie, string value)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Cookie", new CookieHeaderValue(cookie, value).ToString());
            return _ocelotClient.SendAsync(requestMessage);
        }

        private async Task WhenIGetUrlOnTheApiGatewayWithCookie(string url, string cookie, string value)
            => _response = await WhenIGetUrlOnTheApiGateway(url, cookie, value);

        private void WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times, string cookie, string value)
        {
            var tasks = new Task[times];
            for (var i = 0; i < times; i++)
            {
                tasks[i] = GetParallelTask(url, cookie, value);
            }

            Task.WaitAll(tasks);
        }

        private async Task GetParallelTask(string url, string cookie, string value)
        {
            var response = await WhenIGetUrlOnTheApiGateway(url, cookie, value);
            var content = await response.Content.ReadAsStringAsync();
            var count = int.Parse(content);
            count.ShouldBeGreaterThan(0);
        }

        private void ThenTheFirstServiceIsCalled(int expected)
        {
            _counterOne.ShouldBe(expected);
        }

        private void ThenTheSecondServiceIsCalled(int expected)
        {
            _counterTwo.ShouldBe(expected);
        }

        private void GivenProductServiceOneIsRunning(string url, int statusCode)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                try
                {
                    string response;
                    lock (SyncLock)
                    {
                        _counterOne++;
                        response = _counterOne.ToString();
                    }

                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(response);
                }
                catch (Exception exception)
                {
                    await context.Response.WriteAsync(exception.StackTrace);
                }
            });
        }

        private void GivenProductServiceTwoIsRunning(string url, int statusCode)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                try
                {
                    string response;
                    lock (SyncLock)
                    {
                        _counterTwo++;
                        response = _counterTwo.ToString();
                    }

                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(response);
                }
                catch (Exception exception)
                {
                    await context.Response.WriteAsync(exception.StackTrace);
                }
            });
        }
    }
}

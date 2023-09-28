using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Requester;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests
{
    public class HttpClientCachingTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;

        public HttpClientCachingTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void should_cache_one_http_client_same_re_route()
        {
            var port = RandomPortFinder.GetRandomPort();

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

            var cache = new FakeHttpClientCache();

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithFakeHttpClientCache(cache))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheCountShouldBe(cache, 1))
                .BDDfy();
        }

        [Fact]
        public void should_cache_two_http_client_different_re_route()
        {
            var port = RandomPortFinder.GetRandomPort();

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
                        DownstreamPathTemplate = "/two",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/two",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            var cache = new FakeHttpClientCache();

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithFakeHttpClientCache(cache))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/two"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/two"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/two"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheCountShouldBe(cache, 2))
                .BDDfy();
        }

        private static void ThenTheCountShouldBe(FakeHttpClientCache cache, int count)
        {
            cache.Count.ShouldBe(count);
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }

        public class FakeHttpClientCache : IHttpClientCache
        {
            private readonly ConcurrentDictionary<DownstreamRoute, IHttpClient> _httpClientsCache;

            public FakeHttpClientCache()
            {
                _httpClientsCache = new ConcurrentDictionary<DownstreamRoute, IHttpClient>();
            }

            public void Set(DownstreamRoute key, IHttpClient client, TimeSpan expirationTime)
            {
                _httpClientsCache.AddOrUpdate(key, client, (k, oldValue) => client);
            }

            public IHttpClient Get(DownstreamRoute key)
            {
                //todo handle error?
                return _httpClientsCache.TryGetValue(key, out var client) ? client : null;
            }

            public int Count => _httpClientsCache.Count;
        }
    }
}

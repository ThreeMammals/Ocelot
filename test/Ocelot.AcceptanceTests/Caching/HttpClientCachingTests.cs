using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Requester;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.Caching
{
    public sealed class HttpClientCachingTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;
        private const string HelloFromLaura = "Hello from Laura";

        public HttpClientCachingTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        private FileRoute GivenRoute(int port, string template) => new()
        {
            DownstreamPathTemplate = template,
            DownstreamScheme = Uri.UriSchemeHttp,
            DownstreamHostAndPorts =
                [
                    new("localhost", port),
                ],
            UpstreamPathTemplate = template,
            UpstreamHttpMethod =["Get"],
        };

        private FileConfiguration GivenFileConfiguration(params FileRoute[] routes)
        {
            var config = new FileConfiguration();
            config.Routes.AddRange(routes);
            return config;
        }

        [Fact]
        public void Should_cache_one_http_client_same_route()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenFileConfiguration(
                GivenRoute(port, "/"));
            var cache = new FakeHttpClientCache();

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloFromLaura))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithFakeHttpClientCache(cache))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(HelloFromLaura))
                .And(x => ThenTheCountShouldBe(cache, 1))
                .BDDfy();
        }

        [Fact]
        public void Should_cache_two_http_client_different_route()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = GivenFileConfiguration(
                GivenRoute(port, "/"),
                GivenRoute(port, "/two"));
            var cache = new FakeHttpClientCache();

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", HttpStatusCode.OK, HelloFromLaura))
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
                .And(x => _steps.ThenTheResponseBodyShouldBe(HelloFromLaura))
                .And(x => ThenTheCountShouldBe(cache, 2))
                .BDDfy();
        }

        private static void ThenTheCountShouldBe(FakeHttpClientCache cache, int count)
            => cache.Count.ShouldBe(count);

        private void GivenThereIsAServiceRunningOn(string baseUrl, HttpStatusCode statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
            {
                context.Response.StatusCode = (int)statusCode;
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
                => _httpClientsCache = new ConcurrentDictionary<DownstreamRoute, IHttpClient>();

            public void Set(DownstreamRoute key, IHttpClient client, TimeSpan expirationTime)
                => _httpClientsCache.AddOrUpdate(key, client, (k, oldValue) => client);

            public IHttpClient Get(DownstreamRoute key)
                => _httpClientsCache.TryGetValue(key, out var client) ? client : null;

            public int Count => _httpClientsCache.Count;
        }
    }
}

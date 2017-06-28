using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Ocelot.Cache;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cache
{
    public class OutputCacheMiddlewareTests
    {
        private readonly Mock<IOcelotCache<HttpResponseMessage>> _cacheManager;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepo;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private HttpResponseMessage _response;

        public OutputCacheMiddlewareTests()
        {
            _cacheManager = new Mock<IOcelotCache<HttpResponseMessage>>();
            _scopedRepo = new Mock<IRequestScopedDataRepository>();

            _url = "http://localhost:51879";
            var builder = new WebHostBuilder()
                .ConfigureServices(x =>
                {
                    x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                    x.AddLogging();
                    x.AddSingleton(_cacheManager.Object);
                    x.AddSingleton(_scopedRepo.Object);
                    x.AddSingleton<IRegionCreator, RegionCreator>();
                })
                .UseUrls(_url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(_url)
                .Configure(app =>
                {
                    app.UseOutputCacheMiddleware();
                });

            _scopedRepo
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_returned_cached_item_when_it_is_in_cache()
        {
            this.Given(x => x.GivenThereIsACachedResponse(new HttpResponseMessage()))
                .And(x => x.GivenTheDownstreamRouteIs())
                .And(x => x.GivenThereIsADownstreamUrl())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheCacheGetIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_continue_with_pipeline_and_cache_response()
        {
            this.Given(x => x.GivenResponseIsNotCached())
                .And(x => x.GivenTheDownstreamRouteIs())
                .And(x => x.GivenThereAreNoErrors())
                .And(x => x.GivenThereIsADownstreamUrl())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheCacheAddIsCalledCorrectly())
                .BDDfy();
        }


        private void GivenTheDownstreamRouteIs()
        {
            var reRoute = new ReRouteBuilder()
                .WithIsCached(true)
                .WithCacheOptions(new CacheOptions(100, "kanken"))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();
                
            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), reRoute);

            _scopedRepo
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(new OkResponse<DownstreamRoute>(downstreamRoute));
        }

        private void GivenThereAreNoErrors()
        {
            _scopedRepo
                .Setup(x => x.Get<bool>("OcelotMiddlewareError"))
                .Returns(new OkResponse<bool>(false));
        }

        private void GivenThereIsADownstreamUrl()
        {
            _scopedRepo
                .Setup(x => x.Get<string>("DownstreamUrl"))
                .Returns(new OkResponse<string>("anything"));
        }

        private void ThenTheCacheGetIsCalledCorrectly()
        {
            _cacheManager
                .Verify(x => x.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private void ThenTheCacheAddIsCalledCorrectly()
        {
            _cacheManager
                .Verify(x => x.Add(It.IsAny<string>(), It.IsAny<HttpResponseMessage>(), It.IsAny<TimeSpan>(), It.IsAny<string>()), Times.Once);
        }

        private void GivenResponseIsNotCached()
        {
            _scopedRepo
                .Setup(x => x.Get<HttpResponseMessage>("HttpResponseMessage"))
                .Returns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage()));
        }

        private void GivenThereIsACachedResponse(HttpResponseMessage response)
        {
            _response = response;
            _cacheManager
              .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(_response);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }
    }
}

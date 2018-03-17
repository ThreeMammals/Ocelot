using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientBuilderTests : IDisposable
    {
        private readonly HttpClientBuilder _builder;
        private readonly Mock<IDelegatingHandlerHandlerFactory> _factory;
        private IHttpClient _httpClient;
        private HttpResponseMessage _response;
        private DownstreamContext _context;
        private readonly Mock<IHttpClientCache> _cacheHandlers;
        private Mock<IOcelotLogger> _logger;
        private int _count;
        private IWebHost _host;

        public HttpClientBuilderTests()
        {
            _cacheHandlers = new Mock<IHttpClientCache>();
            _logger = new Mock<IOcelotLogger>();
            _factory = new Mock<IDelegatingHandlerHandlerFactory>();
            _builder = new HttpClientBuilder(_factory.Object, _cacheHandlers.Object, _logger.Object);
        }

        [Fact]
        public void should_build_http_client()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false))
                .WithReRouteKey("")
                .Build();

            this.Given(x => GivenTheFactoryReturns())
                .And(x => GivenARequest(reRoute))
                .When(x => WhenIBuild())
                .Then(x => ThenTheHttpClientShouldNotBeNull())
                .BDDfy();
        }

        [Fact]
        public void should_call_delegating_handlers_in_order()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false))
                .WithReRouteKey("")
                .Build();

            var fakeOne = new FakeDelegatingHandler();
            var fakeTwo = new FakeDelegatingHandler();

            var handlers = new List<Func<DelegatingHandler>>()
            { 
                () => fakeOne,
                () => fakeTwo
            };

            this.Given(x => GivenTheFactoryReturns(handlers))
                .And(x => GivenARequest(reRoute))
                .And(x => WhenIBuild())
                .When(x => WhenICallTheClient())
                .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
                .And(x => ThenSomethingIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_re_use_cookies_from_container()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, true, false))
                .WithReRouteKey("")
                .Build();

            this.Given(_ => GivenADownstreamService())
                .And(_ => GivenARequest(reRoute))
                .And(_ => GivenTheFactoryReturnsNothing())
                .And(_ => WhenIBuild())
                .And(_ => WhenICallTheClient("http://localhost:5003"))
                .And(_ => ThenTheCookieIsSet())
                .And(_ => GivenTheClientIsCached())
                .And(_ => WhenIBuild())
                .When(_ => WhenICallTheClient("http://localhost:5003"))
                .Then(_ => ThenTheResponseIsOk())
                .BDDfy();
        }

        private void GivenTheClientIsCached()
        {
            _cacheHandlers.Setup(x => x.Get(It.IsAny<string>())).Returns(_httpClient);
        }

        private void ThenTheCookieIsSet()
        {
            _response.Headers.TryGetValues("Set-Cookie", out var test).ShouldBeTrue();
        }

        private void WhenICallTheClient(string url)
        {
            _response = _httpClient
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, url))
                .GetAwaiter()
                .GetResult();
        }

        private void ThenTheResponseIsOk()
        {
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private void GivenADownstreamService()
        {
            _host = new WebHostBuilder()
                .UseUrls("http://localhost:5003")
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (_count == 0)
                        {
                            context.Response.Cookies.Append("test", "0");
                            context.Response.StatusCode = 200;
                            _count++;
                            return;
                        }
                        if (_count == 1)
                        {
                            if (context.Request.Cookies.TryGetValue("test", out var cookieValue) || context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
                            {
                                context.Response.StatusCode = 200;
                                return;
                            }

                            context.Response.StatusCode = 500;
                        }
                    });
                })
                .Build();

            _host.Start();
        }

        private void GivenARequest(DownstreamReRoute downstream)
        {
            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = downstream,
                DownstreamRequest = new HttpRequestMessage() { RequestUri = new Uri("http://localhost:5003") },
            };

            _context = context;
        }

        private void ThenSomethingIsReturned()
        {
            _response.ShouldNotBeNull();
        }

        private void WhenICallTheClient()
        {
            _response = _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com")).GetAwaiter().GetResult();
        }

        private void ThenTheFakeAreHandledInOrder(FakeDelegatingHandler fakeOne, FakeDelegatingHandler fakeTwo)
        {
            fakeOne.TimeCalled.ShouldBeGreaterThan(fakeTwo.TimeCalled);
        }

        private void GivenTheFactoryReturns()
        {
            var handlers = new List<Func<DelegatingHandler>>(){ () => new FakeDelegatingHandler()};

            _factory
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
        }
        private void GivenTheFactoryReturnsNothing()
        {
            var handlers = new List<Func<DelegatingHandler>>();

            _factory
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
        }

        private void GivenTheFactoryReturns(List<Func<DelegatingHandler>> handlers)
        {
             _factory
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
        }

        private void WhenIBuild()
        {
            _httpClient = _builder.Create(_context);
        }

        private void ThenTheHttpClientShouldNotBeNull()
        {
            _httpClient.ShouldNotBeNull();
        }

        public void Dispose()
        {
            _response?.Dispose();
            _host?.Dispose();
        }
    }
}

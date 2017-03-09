using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Request.Builder;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;
using Ocelot.Configuration;
using Ocelot.Requester.QoS;

namespace Ocelot.UnitTests.Request
{
    public class HttpRequestBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestCreator> _requestBuilder;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<Ocelot.Request.Request> _request;
        private OkResponse<string> _downstreamUrl;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public HttpRequestBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _requestBuilder = new Mock<IRequestCreator>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_qosProviderHouse.Object);
                  x.AddSingleton(_requestBuilder.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpRequestBuilderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {

            var downstreamRoute = new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod("Get")
                    .Build());

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(new NoQoSProvider())))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheRequestBuilderReturns(new Ocelot.Request.Request(new HttpRequestMessage(), true, new NoQoSProvider())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheQosProviderHouseReturns(Response<IQoSProvider> qosProvider)
        {
            _qosProviderHouse
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(qosProvider);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void GivenTheRequestBuilderReturns(Ocelot.Request.Request request)
        {
            _request = new OkResponse<Ocelot.Request.Request>(request);
            _requestBuilder
                .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<IHeaderDictionary>(), 
                It.IsAny<QueryString>(), It.IsAny<string>(), It.IsAny<Ocelot.RequestId.RequestId>(),It.IsAny<bool>(), It.IsAny<IQoSProvider>()))
                .ReturnsAsync(_request);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("Request", _request.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<string>(downstreamUrl);
            _scopedRepository
                .Setup(x => x.Get<string>(It.IsAny<string>()))
                .Returns(_downstreamUrl);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}

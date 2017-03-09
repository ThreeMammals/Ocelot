using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.QueryStrings.Middleware;
using Ocelot.Requester;
using Ocelot.Requester.Middleware;
using Ocelot.Requester.QoS;
using Ocelot.Responder;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpRequesterMiddlewareTests : IDisposable
    {
        private readonly Mock<IHttpRequester> _requester;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<HttpResponseMessage> _response;
        private OkResponse<Ocelot.Request.Request> _request;

        public HttpRequesterMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _requester = new Mock<IHttpRequester>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_requester.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpRequesterMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            this.Given(x => x.GivenTheRequestIs(new Ocelot.Request.Request(new HttpRequestMessage(),true, new NoQoSProvider())))
                .And(x => x.GivenTheRequesterReturns(new HttpResponseMessage()))
                .And(x => x.GivenTheScopedRepoReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedRepoIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheRequesterReturns(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _requester
                .Setup(x => x.GetResponse(It.IsAny<Ocelot.Request.Request>()))
                .ReturnsAsync(_response);
        }

        private void GivenTheScopedRepoReturns()
        {
            _scopedRepository
                .Setup(x => x.Add(It.IsAny<string>(), _response.Data))
                .Returns(new OkResponse());
        }

        private void ThenTheScopedRepoIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("HttpResponseMessage", _response.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheRequestIs(Ocelot.Request.Request request)
        {
            _request = new OkResponse<Ocelot.Request.Request>(request);
            _scopedRepository
                .Setup(x => x.Get<Ocelot.Request.Request>(It.IsAny<string>()))
                .Returns(_request);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}

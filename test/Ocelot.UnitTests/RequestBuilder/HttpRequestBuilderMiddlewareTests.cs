using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.RequestBuilder;
using Ocelot.RequestBuilder.Builder;
using Ocelot.RequestBuilder.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.RequestBuilder
{
    public class HttpRequestBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestBuilder> _requestBuilder;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<Request> _request;
        private OkResponse<string> _downstreamUrl;

        public HttpRequestBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _requestBuilder = new Mock<IRequestBuilder>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
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
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheRequestBuilderReturns(new Request(new HttpRequestMessage(), new CookieContainer())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheRequestBuilderReturns(Request request)
        {
            _request = new OkResponse<Request>(request);
            _requestBuilder
                .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<IHeaderDictionary>(),
                It.IsAny<IRequestCookieCollection>(), It.IsAny<string>(), It.IsAny<string>()))
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

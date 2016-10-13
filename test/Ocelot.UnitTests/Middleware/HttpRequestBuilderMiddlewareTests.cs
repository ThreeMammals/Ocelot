/*
namespace Ocelot.UnitTests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using Library.Infrastructure.DownstreamRouteFinder;
    using Library.Infrastructure.Repository;
    using Library.Infrastructure.RequestBuilder;
    using Library.Infrastructure.Responses;
    using Library.Infrastructure.UrlMatcher;
    using Library.Infrastructure.UrlTemplateReplacer;
    using Library.Middleware;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpRequestBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestBuilder> _requestBuilder;
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<string> _downstreamUrl;

        public HttpRequestBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _requestBuilder = new Mock<IRequestBuilder>();
            _scopedRepository = new Mock<IScopedRequestDataRepository>();

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
            this.Given(x => x.GivenTheDownStreamUrlIs(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), "any old string")))
                .And(x => x.GivenTheRequestBuilderReturns("any old string"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheRequestBuilderReturns(Response<Request> request)
        {
            _downstreamUrl = new OkResponse<string>(downstreamUrl);
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.ReplaceTemplateVariables(It.IsAny<DownstreamRoute>()))
                .Returns(_downstreamUrl);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _scopedRepository
                .Verify(x => x.Add("DownstreamUrl", _downstreamUrl.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamUrlIs(string downstreamRoute)
        {
            _downstreamUrl = new OkResponse<string>(downstreamRoute);
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
*/

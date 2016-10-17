using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    using Library.Middleware;
    using Library.Repository;
    using Library.Responder;
    using Library.Responses;

    public class HttpResponderMiddlewareTests : IDisposable
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<HttpResponseMessage> _response;

        public HttpResponderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _responder = new Mock<IHttpResponder>();
            _scopedRepository = new Mock<IScopedRequestDataRepository>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_codeMapper.Object);
                  x.AddSingleton(_responder.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpResponderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheHttpResponseMessageIs(new HttpResponseMessage()))
                .And(x => x.GivenThereAreNoPipelineErrors())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.TheResponderIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenThereAreNoPipelineErrors()
        {
            _scopedRepository
                .Setup(x => x.Get<bool>(It.IsAny<string>()))
                .Returns(new OkResponse<bool>(false));
        }

        private void TheResponderIsCalledCorrectly()
        {
            _responder
                .Verify(x => x.CreateResponse(It.IsAny<HttpContext>(), _response.Data), Times.Once);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheHttpResponseMessageIs(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _scopedRepository
                .Setup(x => x.Get<HttpResponseMessage>(It.IsAny<string>()))
                .Returns(_response);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}

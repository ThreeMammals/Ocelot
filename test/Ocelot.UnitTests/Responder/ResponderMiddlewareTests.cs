using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responder;
using Ocelot.Responder.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Responder
{
    public class ResponderMiddlewareTests : IDisposable
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<HttpResponseMessage> _response;

        public ResponderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _responder = new Mock<IHttpResponder>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
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
                  app.UseResponderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_not_return_any_errors()
        {
            this.Given(x => x.GivenTheHttpResponseMessageIs(new HttpResponseMessage()))
                .And(x => x.GivenThereAreNoPipelineErrors())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenThereAreNoErrors())
                .BDDfy();
        }

        private void GivenThereAreNoPipelineErrors()
        {
            _scopedRepository
                .Setup(x => x.Get<bool>(It.IsAny<string>()))
                .Returns(new OkResponse<bool>(false));
        }

        private void ThenThereAreNoErrors()
        {
            //todo a better assert?
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

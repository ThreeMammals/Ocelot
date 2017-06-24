using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamUrlCreator;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Errors.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Errors
{
    public class ExceptionHandlerMiddlewareTests
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly string _url;
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _result;

        public ExceptionHandlerMiddlewareTests()
        {
            _url = "http://localhost:52879";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
        }
        
        [Fact]
        public void should_call_next_middleware()
        {
            this.Given(_ => GivenASuccessfulRequest())
                .When(_ => WhenIMakeTheRequest())
                .Then(_ => ThenTheResponseIsOk())
                .BDDfy();
        }

        [Fact]
        public void should_call_return_error()
        {
            this.Given(_ => GivenAnError())
                .When(_ => WhenIMakeTheRequest())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        private void ThenTheResponseIsOk()
        {
            _result.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

         private void ThenTheResponseIsError()
        {
            _result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }

        private void WhenIMakeTheRequest()
        {
            _result = _client.GetAsync("/").Result;
        }

        private void GivenASuccessfulRequest()
        {
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseExceptionHandlerMiddleware();
                  app.Run(async context =>
                    {
                        context.Response.StatusCode = 200;
                    });
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        private void GivenAnError()
        {
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                    app.UseExceptionHandlerMiddleware();
                    app.Use(async (context, next) =>
                    {
                        throw new Exception("BOOM");
                    });
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }
    }
}
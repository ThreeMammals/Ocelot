/*
using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Middleware;
using Ocelot.RequestId.Provider;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Errors
{
    public class GobalErrorHandlerTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger<ExceptionHandlerMiddleware>> _logger;
        private readonly Mock<IRequestIdProvider> _requestIdProvider;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;

        public GobalErrorHandlerTests()
        {
            _url = "http://localhost:51879";
            _logger = new Mock<ILogger<ExceptionHandlerMiddleware>>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _requestIdProvider = new Mock<IRequestIdProvider>();
            var builder = new WebHostBuilder()
                .ConfigureServices(x =>
                {
                    x.AddSingleton(_requestIdProvider.Object);
                    x.AddSingleton(_loggerFactory.Object);
                })
                .UseUrls(_url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(_url)
                .Configure(app =>
                {
                    app.UseExceptionHandlerMiddleware();

                    app.Run(x =>
                    {
                        throw new Exception("BLAM");
                    });
                });

            _loggerFactory
                .Setup(x => x.CreateLogger<ExceptionHandlerMiddleware>())
                .Returns(_logger.Object);

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_catch_exception_and_log()
        {
            this.When(x => x.WhenICallTheMiddleware())
                .And(x => x.TheLoggerIsCalledCorrectly())
                .BDDfy();
        }

        private void TheLoggerIsCalledCorrectly()
        {
            _logger
                .Verify(x => x.LogError(It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }
    }
}
*/

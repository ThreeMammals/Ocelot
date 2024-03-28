using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Polly
{
    public class OcelotBuilderExtensionsTests
    {
        [Fact]
        public void Should_build()
        {
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var services = new ServiceCollection();
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithExceptionsAllowedBeforeBreaking(2)
                .WithDurationOfBreak(200)
                .Build();
            var route = new DownstreamRouteBuilder().WithQosOptions(options)
                .Build();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();
            services
                .AddOcelot(configuration)
                .AddPolly();
            var provider = services.BuildServiceProvider();

            var handler = provider.GetService<QosDelegatingHandlerDelegate>();
            handler.ShouldNotBeNull();

            var delgatingHandler = handler(route, contextAccessor.Object, loggerFactory.Object);
            delgatingHandler.ShouldNotBeNull();
        }
    }
}

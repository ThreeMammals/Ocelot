﻿namespace Ocelot.UnitTests.Polly
{
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DependencyInjection;
    using Ocelot.Logging;
    using Ocelot.Requester;
    using Provider.Polly;
    using Shouldly;
    using Xunit;

    public class OcelotBuilderExtensionsTests
    {
        [Fact]
        public void should_build()
        {
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var services = new ServiceCollection();
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithExceptionsAllowedBeforeBreaking(1)
                .WithDurationOfBreak(200)
                .Build();
            var reRoute = new DownstreamReRouteBuilder().WithQosOptions(options)
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

            var delgatingHandler = handler(reRoute, loggerFactory.Object);
            delgatingHandler.ShouldNotBeNull();
        }
    }
}

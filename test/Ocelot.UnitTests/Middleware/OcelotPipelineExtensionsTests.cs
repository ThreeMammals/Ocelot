using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Middleware.Pipeline;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class OcelotPipelineExtensionsTests
    {
        private OcelotPipelineBuilder _builder;
        private OcelotRequestDelegate _handlers;

        [Fact]
        public void should_set_up_pipeline()
        {
           this.Given(_ => GivenTheDepedenciesAreSetUp())
                .When(_ => WhenIBuild())
                .Then(_ => ThenThePipelineIsBuilt())
                .BDDfy();
        }

        private void ThenThePipelineIsBuilt()
        {
            _handlers.ShouldNotBeNull();
        }

        private void WhenIBuild()
        {
            _handlers = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());
        }

        private void GivenTheDepedenciesAreSetUp()
        {
            IConfigurationBuilder test = new ConfigurationBuilder();
            var root = test.Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(root);
            services.AddOcelot();
            var provider = services.BuildServiceProvider();
            _builder = new OcelotPipelineBuilder(provider);
        }
    }
}
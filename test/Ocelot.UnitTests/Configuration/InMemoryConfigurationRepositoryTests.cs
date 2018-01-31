using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class InMemoryConfigurationRepositoryTests
    {
        private readonly InMemoryOcelotConfigurationRepository _repo;
        private IOcelotConfiguration _config;
        private Response _result;
        private Response<IOcelotConfiguration> _getResult;

        public InMemoryConfigurationRepositoryTests()
        {
            _repo = new InMemoryOcelotConfigurationRepository();
        }

        [Fact]
        public void can_add_config()
        {
            this.Given(x => x.GivenTheConfigurationIs(new FakeConfig("initial", "adminath")))
                .When(x => x.WhenIAddOrReplaceTheConfig())
                .Then(x => x.ThenNoErrorsAreReturned())
                .BDDfy();
        }

        [Fact]
        public void can_get_config()
        {
            this.Given(x => x.GivenThereIsASavedConfiguration())
                .When(x => x.WhenIGetTheConfiguration())
                .Then(x => x.ThenTheConfigurationIsReturned())
                .BDDfy();
        }

        private void ThenTheConfigurationIsReturned()
        {
            _getResult.Data.ReRoutes[0].DownstreamPathTemplate.Value.ShouldBe("initial");
        }

        private void WhenIGetTheConfiguration()
        {
            _getResult = _repo.Get().Result;
        }

        private void GivenThereIsASavedConfiguration()
        {
            GivenTheConfigurationIs(new FakeConfig("initial", "adminath"));
            WhenIAddOrReplaceTheConfig();
        }

        private void GivenTheConfigurationIs(IOcelotConfiguration config)
        {
            _config = config;
        }

        private void WhenIAddOrReplaceTheConfig()
        {
            _result = _repo.AddOrReplace(_config).Result;
        }

        private void ThenNoErrorsAreReturned()
        {
            _result.IsError.ShouldBeFalse();
        }

        class FakeConfig : IOcelotConfiguration
        {
            private readonly string _downstreamTemplatePath;

            public FakeConfig(string downstreamTemplatePath, string administrationPath)
            {
                _downstreamTemplatePath = downstreamTemplatePath;
                AdministrationPath = administrationPath;
            }

            public List<ReRoute> ReRoutes => new List<ReRoute>
            {
                new ReRouteBuilder()
                .WithDownstreamPathTemplate(_downstreamTemplatePath)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
            };

            public string AdministrationPath {get;}

            public ServiceProviderConfiguration ServiceProviderConfiguration => throw new NotImplementedException();

            public string RequestId {get;}
        }
    }
}

using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class ServiceProviderCreatorTests
    {
        private ServiceProviderConfigurationCreator _creator;
        private FileReRoute _reRoute;
        private FileGlobalConfiguration _globalConfig;
        private ServiceProviderConfiguration _result;

        public ServiceProviderCreatorTests()
        {
            _creator = new ServiceProviderConfigurationCreator();
        }

        [Fact]
        public void should_create_service_provider_config()
        {
            var reRoute = new FileReRoute();
            
            var globalConfig = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "127.0.0.1",
                    Port = 1234
                }
            };

            var expected = new ServiceProviderConfigurationBuilder()
                .WithServiceDiscoveryProviderHost("127.0.0.1")
                .WithServiceDiscoveryProviderPort(1234)
                .Build();

            this.Given(x => x.GivenTheFollowingReRoute(reRoute))
                .And(x => x.GivenTheFollowingGlobalConfig(globalConfig))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheConfigIs(expected))
                .BDDfy();
        }

        private void GivenTheFollowingReRoute(FileReRoute fileReRoute)
        {
            _reRoute = fileReRoute;
        }

        private void GivenTheFollowingGlobalConfig(FileGlobalConfiguration fileGlobalConfig)
        {
            _globalConfig = fileGlobalConfig;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_globalConfig);
        }

        private void ThenTheConfigIs(ServiceProviderConfiguration expected)
        {
            _result.ServiceProviderHost.ShouldBe(expected.ServiceProviderHost);
            _result.ServiceProviderPort.ShouldBe(expected.ServiceProviderPort);
        }
    }
}
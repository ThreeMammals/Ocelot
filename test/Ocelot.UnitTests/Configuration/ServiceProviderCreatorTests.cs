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
        private readonly ServiceProviderConfigurationCreator _creator;
        private FileGlobalConfiguration _globalConfig;
        private ServiceProviderConfiguration _result;

        public ServiceProviderCreatorTests()
        {
            _creator = new ServiceProviderConfigurationCreator();
        }

        [Fact]
        public void should_create_service_provider_config()
        {
            var globalConfig = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "127.0.0.1",
                    Port = 1234,
                    Type = "ServiceFabric",
                    Token = "testtoken"
                }
            };

            var expected = new ServiceProviderConfigurationBuilder()
                .WithHost("127.0.0.1")
                .WithPort(1234)
                .WithType("ServiceFabric")
                .WithToken("testtoken")
                .Build();

            this.Given(x => x.GivenTheFollowingGlobalConfig(globalConfig))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheConfigIs(expected))
                .BDDfy();
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
            _result.Host.ShouldBe(expected.Host);
            _result.Port.ShouldBe(expected.Port);
            _result.Token.ShouldBe(expected.Token);
            _result.Type.ShouldBe(expected.Type);
        }
    }
}

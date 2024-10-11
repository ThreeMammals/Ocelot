using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration
{
    public class SecurityOptionsCreatorTests : UnitTest
    {
        private FileSecurityOptions _fileSecurityOptions;
        private SecurityOptions _result;
        private FileGlobalConfiguration _globalConfig;
        private readonly ISecurityOptionsCreator _creator;

        public SecurityOptionsCreatorTests()
        {
            _creator = new SecurityOptionsCreator();
        }

        [Fact]
        public void should_create_route_security_config()
        {
            var ipAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var ipBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var securityOptions = new FileSecurityOptions
            {
                IPAllowedList = ipAllowedList,
                IPBlockedList = ipBlockedList,
            };

            var expected = new SecurityOptions(ipAllowedList, ipBlockedList);

            this.Given(x => x.GivenThe(new FileGlobalConfiguration()))
                .Given(x => x.GivenThe(securityOptions))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheResultIs(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_global_security_config()
        {
            var ipAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var ipBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var globalConfig = new FileGlobalConfiguration
            {
                SecurityOptions = new FileSecurityOptions
                {
                    IPAllowedList = ipAllowedList,
                    IPBlockedList = ipBlockedList,
                },
            };

            var expected = new SecurityOptions(ipAllowedList, ipBlockedList);

            this.Given(x => x.GivenThe(globalConfig))
                .Given(x => x.GivenThe(new FileSecurityOptions()))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheResultIs(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_global_route_security_config()
        {
            var routeIpAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var routeIpBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
            var securityOptions = new FileSecurityOptions
                {
                    IPAllowedList = routeIpAllowedList,
                    IPBlockedList = routeIpBlockedList,
                };

            var globalIpAllowedList = new List<string> { "127.0.0.2", "192.168.1.2" };
            var globalIpBlockedList = new List<string> { "127.0.0.2", "192.168.1.2" };
            var globalConfig = new FileGlobalConfiguration
            {
                SecurityOptions = new FileSecurityOptions
                {
                    IPAllowedList = globalIpAllowedList,
                    IPBlockedList = globalIpBlockedList,
                },
            };

            var expected = new SecurityOptions(routeIpAllowedList, routeIpBlockedList);

            this.Given(x => x.GivenThe(globalConfig))
                .Given(x => x.GivenThe(securityOptions))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheResultIs(expected))
                .BDDfy();
        }

        private void GivenThe(FileSecurityOptions fileSecurityOptions)
        {
            _fileSecurityOptions = fileSecurityOptions;
        }

        private void GivenThe(FileGlobalConfiguration globalConfiguration)
        {
            _globalConfig = globalConfiguration;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileSecurityOptions, _globalConfig);
        }

        private void ThenTheResultIs(SecurityOptions expected)
        {
            for (var i = 0; i < expected.IPAllowedList.Count; i++)
            {
                _result.IPAllowedList[i].ShouldBe(expected.IPAllowedList[i]);
            }

            for (var i = 0; i < expected.IPBlockedList.Count; i++)
            {
                _result.IPBlockedList[i].ShouldBe(expected.IPBlockedList[i]);
            }
        }
    }
}

using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class SecurityOptionsCreatorTests
    {
        private FileReRoute _fileReRoute;
        private FileGlobalConfiguration _fileGlobalConfig;
        private SecurityOptions _result;
        private ISecurityOptionsCreator _creator;

        public SecurityOptionsCreatorTests()
        {
            _creator = new SecurityOptionsCreator();
        }

        [Fact]
        public void should_create_security_config()
        {
            var ipAllowedList = new List<string>() { "127.0.0.1", "192.168.1.1" };
            var ipBlockedList = new List<string>() { "127.0.0.1", "192.168.1.1" };
            var fileReRoute = new FileReRoute
            {
                SecurityOptions = new FileSecurityOptions()
                {
                    IPAllowedList = ipAllowedList,
                    IPBlockedList = ipBlockedList
                }
            };

            var expected = new SecurityOptions(ipAllowedList, ipBlockedList);

            this.Given(x => x.GivenThe(fileReRoute))
              .When(x => x.WhenICreate())
              .Then(x => x.ThenTheResultIs(expected))
              .BDDfy();
        }

        private void GivenThe(FileReRoute reRoute)
        {
            _fileReRoute = reRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileReRoute.SecurityOptions);
        }

        private void ThenTheResultIs(SecurityOptions expected)
        {
            for (int i = 0; i < expected.IPAllowedList.Count; i++)
            {
                _result.IPAllowedList[i].ShouldBe(expected.IPAllowedList[i]);
            }

            for (int i = 0; i < expected.IPBlockedList.Count; i++)
            {
                _result.IPBlockedList[i].ShouldBe(expected.IPBlockedList[i]);
            }
        }
    }
}

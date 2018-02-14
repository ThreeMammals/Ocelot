using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Moq;
using Ocelot.Middleware;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class BaseUrlFinderTests
    {
        private readonly BaseUrlFinder _baseUrlFinder;
        private readonly Mock<IConfiguration> _config;

        private string _result;

        public BaseUrlFinderTests()
        {
            _config = new Mock<IConfiguration>();
            _baseUrlFinder = new BaseUrlFinder(_config.Object);
        }

        [Fact]
        public void should_use_default_base_url()
        {
            this.Given(x => GivenTheConfigBaseUrlIs(""))
              .And(x => GivenTheConfigBaseUrlIs(""))
              .When(x => WhenIFindTheUrl())
              .Then(x => ThenTheUrlIs("http://localhost:5000"))
              .BDDfy();
        }

        [Fact]
        public void should_use_file_config_base_url()
        {
            this.Given(x => GivenTheConfigBaseUrlIs("http://localhost:7000"))
                .And(x => GivenTheConfigBaseUrlIs("http://baseurlfromconfig.com:5181"))
                .When(x => WhenIFindTheUrl())
                .Then(x => ThenTheUrlIs("http://baseurlfromconfig.com:5181"))
                .BDDfy();
        }

        private void GivenTheConfigBaseUrlIs(string configValue)
        {
            var configSection = new ConfigurationSection(new ConfigurationRoot(new List<IConfigurationProvider>{new MemoryConfigurationProvider(new MemoryConfigurationSource())}), "");
            configSection.Value = configValue;
            _config.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSection);
        }

        private void WhenIFindTheUrl()
        {
            _result = _baseUrlFinder.Find();
        }

        private void ThenTheUrlIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}

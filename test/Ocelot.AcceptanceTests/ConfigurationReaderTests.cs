namespace Ocelot.AcceptanceTests
{
    using System.Collections.Generic;
    using Library.Infrastructure.Configuration;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class ConfigurationReaderTests
    {
        private readonly IConfigurationReader _configurationReader;
        private string _configPath;
        private Configuration _result;

        public ConfigurationReaderTests()
        {
            _configurationReader = new ConfigurationReader();
        }

        [Fact]
        public void can_read_configuration()
        {
            const string path = "./ConfigurationReaderTests.can_read_configuration.yaml";

            var expected =
                new Configuration(new List<Route>
                {
                    new Route("productservice/category/{categoryId}/products/{productId}/variants/{variantId}",
                        "https://www.moonpig.com/api/products/{categoryId}/{productId}/{variantId}")
                });

            this.Given(x => x.GivenAConfigPathOf(path))
                .When(x => x.WhenICallTheConfigurationReader())
                .Then(x => x.ThenTheFollowingConfigurationIsReturned(expected))
                .BDDfy();
        }

        private void GivenAConfigPathOf(string configPath)
        {
            _configPath = configPath;
        }

        private void WhenICallTheConfigurationReader()
        {
            _result = _configurationReader.Read(_configPath);
        }

        private void ThenTheFollowingConfigurationIsReturned(Configuration expected)
        {
            _result.Routes[0].Downstream.ShouldBe(expected.Routes[0].Downstream);
            _result.Routes[0].Upstream.ShouldBe(expected.Routes[0].Upstream);
        }
    }
}

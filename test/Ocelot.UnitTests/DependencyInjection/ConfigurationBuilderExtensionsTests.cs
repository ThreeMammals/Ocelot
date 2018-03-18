using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class ConfigurationBuilderExtensionsTests
    {
        private IConfigurationRoot _configuration;
        private string _result;

        [Fact]
        public void should_add_base_url_to_config()
        {
            this.Given(x => GivenTheBaseUrl("test"))
                .When(x => WhenIGet("BaseUrl"))
                .Then(x => ThenTheResultIs("test"))
                .BDDfy();
        }

        private void GivenTheBaseUrl(string baseUrl)
        {
            #pragma warning disable CS0618
            var builder = new ConfigurationBuilder()
                .AddOcelotBaseUrl(baseUrl);
            #pragma warning restore CS0618
            _configuration = builder.Build();
        }

        private void WhenIGet(string key)
        {
            _result = _configuration.GetValue("BaseUrl", "");
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}

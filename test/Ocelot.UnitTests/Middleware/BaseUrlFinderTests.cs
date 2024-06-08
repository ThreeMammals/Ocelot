using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Middleware
{
    public class BaseUrlFinderTests : UnitTest
    {
        private BaseUrlFinder _baseUrlFinder;
        private IConfiguration _config;
        private readonly List<KeyValuePair<string, string>> _data;
        private string _result;

        public BaseUrlFinderTests()
        {
            _data = new List<KeyValuePair<string, string>>();
        }

        [Fact]
        public void should_use_default_base_url()
        {
            this.When(x => WhenIFindTheUrl())
                .Then(x => ThenTheUrlIs("http://localhost:5000"))
                .BDDfy();
        }

        [Fact]
        public void should_use_memory_config_base_url()
        {
            this.Given(x => GivenTheMemoryBaseUrlIs("http://baseurlfromconfig.com:5181"))
                .When(x => WhenIFindTheUrl())
                .Then(x => ThenTheUrlIs("http://baseurlfromconfig.com:5181"))
                .BDDfy();
        }

        [Fact]
        public void should_use_file_config_base_url()
        {
            this.Given(x => GivenTheMemoryBaseUrlIs("http://localhost:7000"))
                .And(x => GivenTheFileBaseUrlIs("http://baseurlfromconfig.com:5181"))
                .When(x => WhenIFindTheUrl())
                .Then(x => ThenTheUrlIs("http://baseurlfromconfig.com:5181"))
                .BDDfy();
        }

        private void GivenTheMemoryBaseUrlIs(string configValue)
        {
            _data.Add(new KeyValuePair<string, string>("BaseUrl", configValue));
        }

        private void GivenTheFileBaseUrlIs(string configValue)
        {
            _data.Add(new KeyValuePair<string, string>("GlobalConfiguration:BaseUrl", configValue));
        }

        private void WhenIFindTheUrl()
        {
            var source = new MemoryConfigurationSource
            {
                InitialData = _data,
            };
            var provider = new MemoryConfigurationProvider(source);
            _config = new ConfigurationRoot(new List<IConfigurationProvider>
            {
                provider,
            });
            _baseUrlFinder = new BaseUrlFinder(_config);
            _result = _baseUrlFinder.Find();
        }

        private void ThenTheUrlIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}

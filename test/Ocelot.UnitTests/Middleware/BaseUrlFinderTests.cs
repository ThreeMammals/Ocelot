using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Middleware;

public class BaseUrlFinderTests : UnitTest
{
    private BaseUrlFinder _baseUrlFinder;
    private IConfiguration _config;
    private readonly List<KeyValuePair<string, string>> _data;

    public BaseUrlFinderTests()
    {
        _data = new List<KeyValuePair<string, string>>();
    }

    [Fact]
    public void Should_use_default_base_url()
    {
        var result = WhenIFindTheUrl();
        result.ShouldBe("http://localhost:5000");
    }

    [Fact]
    public void Should_use_memory_config_base_url()
    {
        GivenTheMemoryBaseUrlIs("http://baseurlfromconfig.com:5181");
        var result = WhenIFindTheUrl();
        result.ShouldBe("http://baseurlfromconfig.com:5181");
    }

    [Fact]
    public void Should_use_file_config_base_url()
    {
        GivenTheMemoryBaseUrlIs("http://localhost:7000");
        GivenTheFileBaseUrlIs("http://baseurlfromconfig.com:5181");
        var result = WhenIFindTheUrl();
        result.ShouldBe("http://baseurlfromconfig.com:5181");
    }

    private void GivenTheMemoryBaseUrlIs(string configValue)
    {
        _data.Add(new KeyValuePair<string, string>("BaseUrl", configValue));
    }

    private void GivenTheFileBaseUrlIs(string configValue)
    {
        _data.Add(new KeyValuePair<string, string>("GlobalConfiguration:BaseUrl", configValue));
    }

    private string WhenIFindTheUrl()
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
        return _baseUrlFinder.Find();
    }
}

using Microsoft.Extensions.Configuration;
using Ocelot.Infrastructure;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Infrastructure;

public class ConfigAwarePlaceholdersTests
{
    private readonly ConfigAwarePlaceholders _placeholders;
    private readonly Mock<IPlaceholders> _basePlaceholders;

    public ConfigAwarePlaceholdersTests()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");
        var configuration = configurationBuilder.Build();

        _basePlaceholders = new Mock<IPlaceholders>();
        _placeholders = new ConfigAwarePlaceholders(configuration, _basePlaceholders.Object);
    }

    [Fact]
    public void Should_return_value_from_underlying_placeholders()
    {
        // Arrange
        var baseUrl = "http://www.bbc.co.uk";
        const string key = "{BaseUrl}";
        _basePlaceholders.Setup(x => x.Get(key)).Returns(new OkResponse<string>(baseUrl));

        // Act
        var result = _placeholders.Get(key);

        // Assert
        result.Data.ShouldBe(baseUrl);
    }

    [Fact]
    public void Should_return_value_from_config_with_same_name_as_placeholder_if_underlying_placeholder_not_found()
    {
        // Arrange
        const string expected = "http://foo-bar.co.uk";
        const string key = "{BaseUrl}";
        _basePlaceholders.Setup(x => x.Get(key)).Returns(new ErrorResponse<string>(new FakeError()));

        // Act
        var result = _placeholders.Get(key);

        // Assert
        result.Data.ShouldBe(expected);
    }

    [Theory]
    [InlineData("{TestConfig}")]
    [InlineData("{TestConfigNested:Child}")]
    public void Should_return_value_from_config(string key)
    {
        // Arrange
        const string expected = "foo";
        _basePlaceholders.Setup(x => x.Get(key)).Returns(new ErrorResponse<string>(new FakeError()));

        // Act
        var result = _placeholders.Get(key);

        // Assert
        result.Data.ShouldBe(expected);
    }

    [Fact]
    public void Should_call_underyling_when_added()
    {
        // Arrange
        const string key = "{Test}";
        Func<Response<string>> func = () => new OkResponse<string>("test)");

        // Act
        _placeholders.Add(key, func);

        // Assert
        _basePlaceholders.Verify(p => p.Add(key, func), Times.Once);
    }

    [Fact]
    public void Should_call_underyling_when_removed()
    {
        // Arrange
        const string key = "{Test}";

        // Act
        _placeholders.Remove(key);

        // Assert
        _basePlaceholders.Verify(p => p.Remove(key), Times.Once);
    }
}

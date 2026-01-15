using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;

namespace Ocelot.UnitTests.Configuration.Validation;

public class HostAndPortValidatorTests : UnitTest
{
    private readonly HostAndPortValidator _validator;

    public HostAndPortValidatorTests()
    {
        _validator = new HostAndPortValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_be_invalid_because_host_empty(string host)
    {
        // Arrange
        var hostAndPort = new FileHostAndPort
        {
            Host = host,
        };

        // Act
        var result = _validator.Validate(hostAndPort);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using Route.Host or Ocelot cannot find your service!");
    }

    [Fact]
    public void Should_be_valid_because_host_set()
    {
        // Arrange
        var hostAndPort = new FileHostAndPort
        {
            Host = "test",
        };

        // Act
        var result = _validator.Validate(hostAndPort);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}

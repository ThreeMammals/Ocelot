using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class LoadBalancerOptionsCreatorTests : UnitTest
{
    private readonly LoadBalancerOptionsCreator _creator = new();

    [Fact]
    public void Should_create()
    {
        // Arrange
        var options = new FileLoadBalancerOptions
        {
            Type = "test",
            Key = "west",
            Expiry = 1,
        };

        // Act
        var result = _creator.Create(options);

        // Assert
        result.Type.ShouldBe(options.Type);
        result.Key.ShouldBe(options.Key);
        result.ExpiryInMs.ShouldBe(options.Expiry);
    }
}

using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsCreatorTests : UnitTest
{
    private readonly QoSOptionsCreator _creator = new();

    [Fact]
    public void Should_create_qos_options()
    {
        // Arrange
        var route = new FileRoute
        {
            QoSOptions = new FileQoSOptions
            {
                ExceptionsAllowedBeforeBreaking = 1,
                DurationOfBreak = 1,
                TimeoutValue = 1,
            },
        };
        var expected = new QoSOptionsBuilder()
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithTimeoutValue(1)
            .Build();

        // Act
        var result = _creator.Create(route.QoSOptions);

        // Assert
        result.DurationOfBreak.ShouldBe(expected.DurationOfBreak);
        result.ExceptionsAllowedBeforeBreaking.ShouldBe(expected.ExceptionsAllowedBeforeBreaking);
        result.TimeoutValue.ShouldBe(expected.TimeoutValue);
    }
}

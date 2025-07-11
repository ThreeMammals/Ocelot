using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileQoSOptionsTests
{
    [Fact]
    [Trait("PR", "2073")]
    [Trait("PR", "2081")]
    public void Ctor_Default_AllPropertiesAreNull()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Null(actual.DurationOfBreak);
        Assert.Null(actual.ExceptionsAllowedBeforeBreaking);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.TimeoutValue);
    }
}

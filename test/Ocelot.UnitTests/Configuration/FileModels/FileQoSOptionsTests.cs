using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileQoSOptionsTests
{
    [Fact]
    [Trait("Bug", "1833")]
    [Trait("Feat", "2073")]
    public void Cstor_Default_NoTimeoutValue()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Null(actual.TimeoutValue);
    }

    [Fact]
    public void Cstor_Default_AssignedZeroToExceptionsAllowedBeforeBreaking()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Equal(0, actual.ExceptionsAllowedBeforeBreaking);
    }

    [Fact]
    public void Cstor_Default_AssignedOneToDurationOfBreak()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Equal(1, actual.DurationOfBreak);
    }
}

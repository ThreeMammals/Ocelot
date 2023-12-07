using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileQoSOptionsTests
{
    [Fact(DisplayName = "1833: Default constructor must assign zero to the TimeoutValue property")]
    public void Cstor_Default_AssignedZeroToTimeoutValue()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Equal(0, actual.TimeoutValue);
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

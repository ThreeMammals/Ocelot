using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileGlobalWebSocketOptionsTests
{
    [Fact]
    public void Ctor_Default()
    {
        // Arrange, Act
        FileGlobalWebSocketOptions actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void Ctor_FileWebSocketOptions()
    {
        // Arrange
        FileWebSocketOptions from = new() { BufferSize = 65536 };

        // Act
        FileGlobalWebSocketOptions actual = new(from);

        // Assert
        Assert.Null(actual.RouteKeys);
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equal(65536, actual.BufferSize);
    }

    [Fact]
    public void Ctor_FileWebSocketOptions_Null()
    {
        // Arrange, Act
        FileGlobalWebSocketOptions actual = new((FileWebSocketOptions)null);

        // Assert
        Assert.Null(actual.BufferSize);
    }
}

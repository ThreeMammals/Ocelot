using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileWebSocketOptionsTests
{
    [Fact]
    public void Ctor_Default()
    {
        // Arrange, Act
        FileWebSocketOptions actual = new();

        // Assert
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void Ctor_FileWebSocketOptions()
    {
        // Arrange
        FileWebSocketOptions from = new() { BufferSize = 65536 };

        // Act
        FileWebSocketOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equal(65536, actual.BufferSize);
    }

    [Fact]
    public void Ctor_FileWebSocketOptions_Null()
    {
        // Arrange, Act
        FileWebSocketOptions actual = new((FileWebSocketOptions)null);

        // Assert
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void Ctor_WebSocketOptions()
    {
        // Arrange
        WebSocketOptions from = new(65536);

        // Act
        FileWebSocketOptions actual = new(from);

        // Assert
        Assert.Equal(65536, actual.BufferSize);
    }

    [Fact]
    public void Ctor_WebSocketOptions_Null()
    {
        // Arrange, Act
        FileWebSocketOptions actual = new((WebSocketOptions)null);

        // Assert
        Assert.Null(actual.BufferSize);
    }
}

using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

public class WebSocketOptionsTests
{
    [Fact]
    public void Ctor_Copy()
    {
        // Arrange
        WebSocketOptions from = new(65536);

        // Act
        WebSocketOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equal(65536, actual.BufferSize);
    }

    [Fact]
    public void Ctor_Copy_Null()
    {
        // Arrange, Act
        WebSocketOptions actual = new((WebSocketOptions)null);

        // Assert
        Assert.Null(actual.BufferSize);
    }
}

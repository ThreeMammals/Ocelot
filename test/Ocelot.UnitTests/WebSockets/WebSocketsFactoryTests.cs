using Ocelot.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketsFactoryTests
{
    [Fact]
    public void CreateClient_Created()
    {
        // Arrange
        WebSocketsFactory factory = new();

        // Act
        var actual = factory.CreateClient();

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<ClientWebSocketProxy>(actual);
    }
}

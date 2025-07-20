using Ocelot.Infrastructure;

namespace Ocelot.UnitTests.Infrastructure;

public class InMemoryBusTests
{
    private readonly InMemoryBus<object> _bus = new();

    [Fact]
    public async Task Should_publish_with_delay()
    {
        // Arrange
        var called = false;
        _bus.Subscribe(x => called = true);

        // Act
        _bus.Publish(new object(), 1);
        await Task.Delay(100);

        // Assert
        called.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_be_publish_yet_as_no_delay_in_caller()
    {
        // Arrange
        var called = false;
        _bus.Subscribe(x => called = true);

        // Act
        _bus.Publish(new object(), 1);

        // Assert
        called.ShouldBeFalse();
    }
}

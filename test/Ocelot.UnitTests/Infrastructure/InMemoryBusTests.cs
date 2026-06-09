using Ocelot.Infrastructure;
using System.Reflection;

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
        await Task.Delay(100, TestContext.Current.CancellationToken);

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

    [Fact]
    public void Should_create_processing_thread_as_background_thread()
    {
        // Arrange
        var bus = new InMemoryBus<object>();

        // Act - Get the processing thread via reflection
        var processingField = typeof(InMemoryBus<object>).GetField("_processing", 
            BindingFlags.Instance | BindingFlags.NonPublic);
        var thread = processingField?.GetValue(bus) as Thread;

        // Assert - the thread should be marked as background
        thread.ShouldNotBeNull();
        thread.IsBackground.ShouldBeTrue();
    }
}

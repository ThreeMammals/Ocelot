using Ocelot.Infrastructure;
using System.Reflection;

namespace Ocelot.UnitTests.Infrastructure;

public class InMemoryBusTests : UnitTest
{
    private readonly InMemoryBus<object> _bus = new();

    [Fact]
    public async Task Publish_WithDelay_Published()
    {
        // Arrange
        var called = false;
        _bus.Subscribe(x => called = true);

        // Act
        _bus.Publish(new object(), 1);
        await Task.Delay(100, CancelMe);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void Publish_WithoutDelay_NotYetPublished()
    {
        // Arrange
        var called = false;
        _bus.Subscribe(x => called = true);

        // Act
        _bus.Publish(new object(), 1);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void Ctor_ViaReflection_CreatedProcessingThreadAsBackgroundThread()
    {
        // Arrange, Act - Get the processing thread via reflection
        var processingField = typeof(InMemoryBus<object>).GetField("_processing", BindingFlags.Instance | BindingFlags.NonPublic);
        var thread = processingField?.GetValue(_bus) as Thread;

        // Assert - the thread should be marked as background
        Assert.NotNull(thread);
        Assert.True(thread.IsBackground);
    }
}

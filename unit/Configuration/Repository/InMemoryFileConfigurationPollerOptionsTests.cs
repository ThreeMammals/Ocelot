using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration.Repository;

public class InMemoryFileConfigurationPollerOptionsTests : UnitTest
{
    [Fact]
    public void Delay_Returns_DefaultValue()
    {
        InMemoryFileConfigurationPollerOptions sut = new();

        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, sut.Delay());
    }

    [Fact]
    public void Delay_Returns_CustomValue()
    {
        var originalDelay = InMemoryFileConfigurationPollerOptions.DelayMilliseconds;
        try
        {
            InMemoryFileConfigurationPollerOptions.DelayMilliseconds = 5000;
            InMemoryFileConfigurationPollerOptions sut = new();

            Assert.Equal(5000, sut.Delay());
        }
        finally
        {
            InMemoryFileConfigurationPollerOptions.DelayMilliseconds = originalDelay;
        }
    }

    [Fact]
    public async Task DelayAsync_Returns_DefaultValue()
    {
        InMemoryFileConfigurationPollerOptions sut = new();

        var result = await sut.DelayAsync(CancelMe);

        Assert.Equal(InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds, result);
    }

    [Fact]
    public async Task DelayAsync_Returns_CustomValue()
    {
        var originalDelay = InMemoryFileConfigurationPollerOptions.DelayMilliseconds;
        try
        {
            InMemoryFileConfigurationPollerOptions.DelayMilliseconds = 3000;
            InMemoryFileConfigurationPollerOptions sut = new();

            var result = await sut.DelayAsync(CancelMe);

            Assert.Equal(3000, result);
        }
        finally
        {
            InMemoryFileConfigurationPollerOptions.DelayMilliseconds = originalDelay;
        }
    }

    [Fact]
    public void DefaultDelayMilliseconds_IsOneThousand()
    {
        Assert.Equal(1000, InMemoryFileConfigurationPollerOptions.DefaultDelayMilliseconds);
    }
}

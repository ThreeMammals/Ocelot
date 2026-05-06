using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration.Repository;

public class InMemoryFileConfigurationPollerOptionsTests
{
    [Fact]
    public void Delay()
    {
        InMemoryFileConfigurationPollerOptions sut = new();

        Assert.Equal(1000, sut.Delay());
    }
}

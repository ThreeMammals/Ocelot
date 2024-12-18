using Ocelot.Configuration.ChangeTracking;

namespace Ocelot.UnitTests.Configuration.ChangeTracking;

public class OcelotConfigurationChangeTokenSourceTests : UnitTest
{
    private readonly OcelotConfigurationChangeTokenSource _source;

    public OcelotConfigurationChangeTokenSourceTests()
    {
        _source = new OcelotConfigurationChangeTokenSource();
    }

    [Fact]
    public void Should_activate_change_token()
    {
        // Arrange, Act
        _source.Activate();

        // Assert
        _source.ChangeToken.HasChanged.ShouldBeTrue();
    }
}

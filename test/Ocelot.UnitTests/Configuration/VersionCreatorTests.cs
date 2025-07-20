using Ocelot.Configuration.Creator;

namespace Ocelot.UnitTests.Configuration;

public class VersionCreatorTests : UnitTest
{
    private readonly HttpVersionCreator _creator = new();

    [Fact]
    public void Should_create_version_based_on_input()
    {
        // Arrange, Act
        var result = _creator.Create("2.0");

        // Assert
        result.Major.ShouldBe(2);
        result.Minor.ShouldBe(0);
    }

    [Fact]
    public void Should_default_to_version_one_point_one()
    {
        // Arrange, Act
        var result = _creator.Create(string.Empty);

        // Assert
        result.Major.ShouldBe(1);
        result.Minor.ShouldBe(1);
    }
}

using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

[Trait("Feat", "738")]
public class DefaultMetadataCreatorTests : UnitTest
{
    private readonly DefaultMetadataCreator _sut = new();
    private static readonly Dictionary<string, string> Empty = new();

    [Fact]
    public void Should_return_empty_metadata()
    {
        // Arrange, Act
        var result = _sut.Create(Empty, new());

        // Assert
        result.Metadata.Keys.ShouldBeEmpty();
    }

    [Fact]
    public void Should_return_global_metadata()
    {
        // Arrange
        var global = GivenSomeMetadataInGlobalConfiguration();

        // Act
        var result = _sut.Create(Empty, global);

        // Assert
        ThenDownstreamMetadataMustContain(result, "foo", "bar");
    }

    [Fact]
    public void Should_return_route_metadata()
    {
        // Arrange
        var metadata = GivenSomeMetadataInRoute();

        // Act
        var result = _sut.Create(metadata, new());

        // Assert
        ThenDownstreamMetadataMustContain(result, "foo", "baz");
    }

    [Fact]
    public void Should_overwrite_global_metadata()
    {
        // Arrange
        var global = GivenSomeMetadataInGlobalConfiguration();
        var metadata = GivenSomeMetadataInRoute();

        // Act
        var result = _sut.Create(metadata, global);

        // Assert
        ThenDownstreamMetadataMustContain(result, "foo", "baz");
    }

    private static FileGlobalConfiguration GivenSomeMetadataInGlobalConfiguration() => new()
    {
        MetadataOptions = new()
        {
            Metadata = new Dictionary<string, string>
            {
                ["foo"] = "bar",
            },
        },
    };

    private static Dictionary<string, string> GivenSomeMetadataInRoute() => new()
    {
        ["foo"] = "baz",
    };

    private static void ThenDownstreamMetadataMustContain(MetadataOptions result, string key, string value)
    {
        result.Metadata.Keys.ShouldContain(key);
        result.Metadata[key].ShouldBeEquivalentTo(value);
    }
}

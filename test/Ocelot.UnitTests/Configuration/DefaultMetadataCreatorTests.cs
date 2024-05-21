using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

[Trait("Feat", "738")]
public class DefaultMetadataCreatorTests : UnitTest
{
    private FileGlobalConfiguration _globalConfiguration;
    private Dictionary<string, string> _metadataInRoute;
    private MetadataOptions _result;
    private readonly DefaultMetadataCreator _sut = new();

    [Fact]
    public void Should_return_empty_metadata()
    {
        // Arrange
        GivenEmptyMetadataInGlobalConfiguration();
        GivenEmptyMetadataInRoute();

        // Act
        WhenICreate();

        // Assert
        ThenDownstreamRouteMetadataMustBeEmpty();
    }

    [Fact]
    public void Should_return_global_metadata()
    {
        // Arrange
        GivenSomeMetadataInGlobalConfiguration();
        GivenEmptyMetadataInRoute();

        // Act
        WhenICreate();

        // Assert
        ThenDownstreamMetadataMustContain("foo", "bar");
    }

    [Fact]
    public void Should_return_route_metadata()
    {
        // Arrange
        GivenEmptyMetadataInGlobalConfiguration();
        GivenSomeMetadataInRoute();

        // Act
        WhenICreate();

        // Assert
        ThenDownstreamMetadataMustContain("foo", "baz");
    }

    [Fact]
    public void Should_overwrite_global_metadata()
    {
        // Arrange
        GivenSomeMetadataInGlobalConfiguration();
        GivenSomeMetadataInRoute();

        // Act
        WhenICreate();

        // Assert
        ThenDownstreamMetadataMustContain("foo", "baz");
    }

    private void WhenICreate()
    {
        _result = _sut.Create( _metadataInRoute, _globalConfiguration);
    }

    private void GivenEmptyMetadataInGlobalConfiguration()
    {
        _globalConfiguration = new FileGlobalConfiguration();
    }

    private void GivenSomeMetadataInGlobalConfiguration()
    {
        _globalConfiguration = new FileGlobalConfiguration
        {
            MetadataOptions = new FileMetadataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
            },
        };
    }

    private void GivenEmptyMetadataInRoute()
    {
        _metadataInRoute = new Dictionary<string, string>();
    }

    private void GivenSomeMetadataInRoute()
    {
        _metadataInRoute = new Dictionary<string, string>
        {
            ["foo"] = "baz",
        };
    }

    private void ThenDownstreamRouteMetadataMustBeEmpty()
    {
        _result.Metadata.Keys.ShouldBeEmpty();
    }

    private void ThenDownstreamMetadataMustContain(string key, string value)
    {
        _result.Metadata.Keys.ShouldContain(key);
        _result.Metadata[key].ShouldBeEquivalentTo(value);
    }
}

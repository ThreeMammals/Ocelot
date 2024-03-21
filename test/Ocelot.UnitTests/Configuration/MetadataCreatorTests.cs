using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class MetadataCreatorTests
{
    private FileGlobalConfiguration _globalConfiguration;
    private Dictionary<string, string> _metadataInRoute;
    private IDictionary<string, string> _result;
    private readonly MetadataCreator _sut = new();

    [Fact]
    public void should_return_empty_metadata()
    {
        this.Given(_ => GivenEmptyMetadataInGlobalConfiguration())
            .Given(_ => GivenEmptyMetadataInRoute())
            .When(_ => WhenICreate())
            .Then(_ => ThenDownstreamRouteMetadataMustBeEmpty());
    }

    [Fact]
    public void should_return_global_metadata()
    {
        this.Given(_ => GivenSomeMetadataInGlobalConfiguration())
            .Given(_ => GivenEmptyMetadataInRoute())
            .When(_ => WhenICreate())
            .Then(_ => ThenDownstreamMetadataMustContain("foo", "bar"));
    }

    [Fact]
    public void should_return_route_metadata()
    {
        this.Given(_ => GivenEmptyMetadataInGlobalConfiguration())
            .Given(_ => GivenSomeMetadataInRoute())
            .When(_ => WhenICreate())
            .Then(_ => ThenDownstreamMetadataMustContain("foo", "baz"));
    }

    [Fact]
    public void should_overwrite_global_metadata()
    {
        this.Given(_ => GivenSomeMetadataInGlobalConfiguration())
            .Given(_ => GivenSomeMetadataInRoute())
            .When(_ => WhenICreate())
            .Then(_ => ThenDownstreamMetadataMustContain("foo", "baz"));
    }

    private void WhenICreate()
    {
        _result = _sut.Create(_metadataInRoute, _globalConfiguration);
    }

    private void GivenEmptyMetadataInGlobalConfiguration()
    {
        _globalConfiguration = new FileGlobalConfiguration();
    }

    private void GivenSomeMetadataInGlobalConfiguration()
    {
        _globalConfiguration = new FileGlobalConfiguration()
        {
            Metadata = new Dictionary<string, string>
            {
                ["foo"] = "bar",
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
        _result.Keys.ShouldBeEmpty();
    }

    private void ThenDownstreamMetadataMustContain(string key, string value)
    {
        _result.Keys.ShouldContain(key);
        _result[key].ShouldBeEquivalentTo(value);
    }
}

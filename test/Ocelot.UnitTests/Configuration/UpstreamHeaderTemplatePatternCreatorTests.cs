using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamHeaderTemplatePatternCreatorTests
{
    private FileRoute _fileRoute;
    private readonly UpstreamHeaderTemplatePatternCreator _creator;
    private IDictionary<string, UpstreamHeaderTemplate> _result;

    public UpstreamHeaderTemplatePatternCreatorTests()
    {
        _creator = new();
    }

    [Fact]
    public void Should_create_pattern_without_placeholders()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "a text without placeholders",
            },
        };
        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^(?i)a text without placeholders$");
    }

    [Fact]
    public void Should_create_pattern_case_sensitive()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            RouteIsCaseSensitive = true,
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "a text without placeholders",
            },
        };
        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^a text without placeholders$");
    }

    [Fact]
    public void Should_create_pattern_with_placeholder_in_the_beginning()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "{header:start}rest of the text",
            },
        };
        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^(?i)(?<start>.+)rest of the text$");
    }

    [Fact]
    public void Should_create_pattern_with_placeholder_at_the_end()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "rest of the text{header:end}",
            },
        };
        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^(?i)rest of the text(?<end>.+)$");
    }

    [Fact]
    public void Should_create_pattern_with_placeholder_only()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "{header:countrycode}",
            },
        };

        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^(?i)(?<countrycode>.+)$");
    }

    [Fact]
    public void Should_crate_pattern_with_more_placeholders()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                ["country"] = "any text {header:cc} and other {header:version} and {header:bob} the end",
            },
        };
        GivenTheFollowingFileRoute(fileRoute);

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ThenTheFollowingIsReturned("country", "^(?i)any text (?<cc>.+) and other (?<version>.+) and (?<bob>.+) the end$");
    }

    private void GivenTheFollowingFileRoute(FileRoute fileRoute)
    {
        _fileRoute = fileRoute;
    }

    private void WhenICreateTheTemplatePattern()
    {
        _result = _creator.Create(_fileRoute);
    }

    private void ThenTheFollowingIsReturned(string headerKey, string expected)
    {
        _result[headerKey].Template.ShouldBe(expected);
    }
}

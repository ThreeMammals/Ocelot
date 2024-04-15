using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamHeaderTemplatePatternCreatorTests
{
    private readonly UpstreamHeaderTemplatePatternCreator _creator;

    public UpstreamHeaderTemplatePatternCreatorTests()
    {
        _creator = new();
    }

    [Trait("PR", "1312")]
    [Trait("Feat", "360")]
    [Theory(DisplayName = "Should create pattern")]
    [InlineData("country", "a text without placeholders", "^(?i)a text without placeholders$", " without placeholders")]
    [InlineData("country", "a text without placeholders", "^a text without placeholders$", " Route is case sensitive", true)]
    [InlineData("country", "{header:start}rest of the text", "^(?i)(?<start>.+)rest of the text$", " with placeholder in the beginning")]
    [InlineData("country", "rest of the text{header:end}", "^(?i)rest of the text(?<end>.+)$", " with placeholder at the end")]
    [InlineData("country", "{header:countrycode}", "^(?i)(?<countrycode>.+)$", " with placeholder only")]
    [InlineData("country", "any text {header:cc} and other {header:version} and {header:bob} the end", "^(?i)any text (?<cc>.+) and other (?<version>.+) and (?<bob>.+) the end$", " with more placeholders")]
    public void Create_WithUpstreamHeaderTemplates_ShouldCreatePattern(string key, string template, string expected, string withMessage, bool? isCaseSensitive = null)
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            RouteIsCaseSensitive = isCaseSensitive ?? false,
            UpstreamHeaderTemplates = new Dictionary<string, string>
            {
                [key] = template,
            },
        };

        // Act
        var actual = _creator.Create(fileRoute);

        // Assert
        var message = nameof(Create_WithUpstreamHeaderTemplates_ShouldCreatePattern).Replace('_', ' ') + withMessage;
        actual[key].ShouldNotBeNull()
            .Template.ShouldBe(expected, message);
    }
}

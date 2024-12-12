using Ocelot.Configuration;
using Ocelot.Configuration.Parser;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration;

public class ClaimToThingConfigurationParserTests : UnitTest
{
    private readonly ClaimToThingConfigurationParser _parser;

    public ClaimToThingConfigurationParserTests()
    {
        _parser = new ClaimToThingConfigurationParser();
    }

    [Fact]
    public void Returns_no_instructions_error()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            {"CustomerId", string.Empty},
        };

        // Act
        var result = WhenICallTheExtractor(dictionary);

        // Assert
        ThenAnErrorIsReturned(result, new ErrorResponse<ClaimToThing>(
            new List<Error>
            {
                new NoInstructionsError(">"),
            }));
    }

    [Fact]
    public void Returns_no_instructions_not_for_claims_error()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            {"CustomerId", "Cheese[CustomerId] > value"},
        };

        // Act
        var result = WhenICallTheExtractor(dictionary);

        // Assert
        ThenAnErrorIsReturned(result, new ErrorResponse<ClaimToThing>(new List<Error>
            {
                new InstructionNotForClaimsError(),
            }));
    }

    [Fact]
    public void Can_parse_entry_to_work_out_properties_with_key()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            {"CustomerId", "Claims[CustomerId] > value"},
        };

        // Act
        var result = WhenICallTheExtractor(dictionary);

        // Assert
        ThenTheClaimParserPropertiesAreReturned(result,
            new OkResponse<ClaimToThing>(new ClaimToThing("CustomerId", "CustomerId", string.Empty, 0)));
    }

    [Fact]
    public void Can_parse_entry_to_work_out_properties_with_key_delimiter_and_index()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            {"UserId", "Claims[Subject] > value[0] > |"},
        };

        // Act
        var result = WhenICallTheExtractor(dictionary);

        // Assert
        ThenTheClaimParserPropertiesAreReturned(result,
            new OkResponse<ClaimToThing>(new ClaimToThing("UserId", "Subject", "|", 0)));
    }

    private static void ThenAnErrorIsReturned(Response<ClaimToThing> result, Response<ClaimToThing> expected)
    {
        result.IsError.ShouldBe(expected.IsError);
        result.Errors[0].ShouldBeOfType(expected.Errors[0].GetType());
    }

    private static void ThenTheClaimParserPropertiesAreReturned(Response<ClaimToThing> result, Response<ClaimToThing> expected)
    {
        result.Data.NewKey.ShouldBe(expected.Data.NewKey);
        result.Data.Delimiter.ShouldBe(expected.Data.Delimiter);
        result.Data.Index.ShouldBe(expected.Data.Index);
        result.IsError.ShouldBe(expected.IsError);
    }

    private Response<ClaimToThing> WhenICallTheExtractor(Dictionary<string, string> dictionary)
    {
        var first = dictionary.First();
        return _parser.Extract(first.Key, first.Value);
    }
}

using Ocelot.Errors;
using Ocelot.Infrastructure.Claims;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Infrastructure;

public class ClaimParserTests : UnitTest
{
    private readonly ClaimsParser _claimsParser;
    private readonly List<Claim> _claims;

    public ClaimParserTests()
    {
        _claimsParser = new();
        _claims = new();
    }

    [Fact]
    public void Can_parse_claims_dictionary_access_string_returning_value_to_function()
    {
        // Arrange
        const string key = "CustomerId";
        _claims.Add(new Claim(key, "1234"));

        // Act
        var result = _claimsParser.GetValue(_claims, key, default, default);

        // Assert
        ThenTheResultIs(result, new OkResponse<string>("1234"));
    }

    [Fact]
    public void Should_return_error_response_when_cannot_find_requested_claim()
    {
        // Arrange
        const string key = "CustomerId";
        _claims.Add(new Claim("BallsId", "1234"));

        // Act
        var result = _claimsParser.GetValue(_claims, key, default, default);

        // Assert
        ThenTheResultIs(result, new ErrorResponse<string>(new List<Error>
        {
            new CannotFindClaimError($"Cannot find claim for key: {key}"),
        }));
    }

    [Fact]
    public void Can_parse_claims_dictionary_access_string_using_delimiter_and_retuning_at_correct_index()
    {
        // Arrange
        const string key = "Subject";
        _claims.Add(new Claim("Subject", "registered|4321"));

        // Act
        var result = _claimsParser.GetValue(_claims, key, "|", 1);

        // Assert
        ThenTheResultIs(result, new OkResponse<string>("4321"));
    }

    [Fact]
    public void Should_return_error_response_if_index_too_large()
    {
        // Arrange
        const string key = "Subject";
        const string delimiter = "|";
        const int index = 24;
        _claims.Add(new Claim("Subject", "registered|4321"));

        // Act
        var result = _claimsParser.GetValue(_claims, key, delimiter, index);

        // Assert
        ThenTheResultIs(result, new ErrorResponse<string>(new List<Error>
        {
            new CannotFindClaimError($"Cannot find claim for key: {key}, delimiter: {delimiter}, index: {index}"),
        }));
    }

    [Fact]
    public void Should_return_error_response_if_index_too_small()
    {
        // Arrange
        const string key = "Subject";
        const string delimiter = "|";
        const int index = -1;
        _claims.Add(new Claim("Subject", "registered|4321"));

        // Act
        var result = _claimsParser.GetValue(_claims, key, delimiter, index);

        // Assert
        ThenTheResultIs(result, new ErrorResponse<string>(new List<Error>
        {
            new CannotFindClaimError($"Cannot find claim for key: {key}, delimiter: {delimiter}, index: {index}"),
        }));
    }

    private static void ThenTheResultIs(Response<string> actual, Response<string> expected)
    {
        actual.Data.ShouldBe(expected.Data);
        actual.IsError.ShouldBe(expected.IsError);
    }
}

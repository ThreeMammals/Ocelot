using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Parser;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration;

public class ClaimsToThingCreatorTests : UnitTest
{
    private readonly Mock<IClaimToThingConfigurationParser> _configParser;
    private Dictionary<string, string> _claimsToThings;
    private readonly ClaimsToThingCreator _claimsToThingsCreator;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private List<ClaimToThing> _result;
    private readonly Mock<IOcelotLogger> _logger;

    public ClaimsToThingCreatorTests()
    {
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory
            .Setup(x => x.CreateLogger<ClaimsToThingCreator>())
            .Returns(_logger.Object);
        _configParser = new Mock<IClaimToThingConfigurationParser>();
        _claimsToThingsCreator = new ClaimsToThingCreator(_configParser.Object, _loggerFactory.Object);
    }

    [Fact]
    public void Should_return_claims_to_things()
    {
        // Arrange
        var userInput = new Dictionary<string, string>
        {
            {"CustomerId", "Claims[CustomerId] > value"},
        };
        var claimsToThing = new OkResponse<ClaimToThing>(new ClaimToThing("CustomerId", "CustomerId", string.Empty, 0));
        GivenTheFollowingDictionary(userInput);
        GivenTheConfigHeaderExtractorReturns(claimsToThing);

        // Act
        WhenIGetTheThings();

        // Assert
        ThenTheConfigParserIsCalledCorrectly();
        ThenClaimsToThingsAreReturned();
    }

    [Fact]
    public void Should_log_error_if_cannot_parse_claim_to_thing()
    {
        // Arrange
        var userInput = new Dictionary<string, string>
        {
            {"CustomerId", "Claims[CustomerId] > value"},
        };
        var claimsToThing = new ErrorResponse<ClaimToThing>(It.IsAny<Error>());
        GivenTheFollowingDictionary(userInput);
        GivenTheConfigHeaderExtractorReturns(claimsToThing);

        // Act
        WhenIGetTheThings();

        // Assert
        ThenTheConfigParserIsCalledCorrectly();
        ThenNoClaimsToThingsAreReturned();
    }

    private void ThenClaimsToThingsAreReturned()
    {
        _result.Count.ShouldBeGreaterThan(0);
    }

    private void GivenTheFollowingDictionary(Dictionary<string, string> claimsToThings)
    {
        _claimsToThings = claimsToThings;
    }

    private void GivenTheConfigHeaderExtractorReturns(Response<ClaimToThing> expected)
    {
        _configParser
            .Setup(x => x.Extract(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(expected);
    }

    private void ThenNoClaimsToThingsAreReturned()
    {
        _result.Count.ShouldBe(0);
    }

    private void WhenIGetTheThings()
    {
        _result = _claimsToThingsCreator.Create(_claimsToThings);
    }

    private void ThenTheConfigParserIsCalledCorrectly()
    {
        _configParser
            .Verify(x => x.Extract(_claimsToThings.First().Key, _claimsToThings.First().Value), Times.Once);
    }
}

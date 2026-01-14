using Ocelot.Errors;
using Ocelot.Responder;
namespace Ocelot.UnitTests.Responder;

public class ErrorsToHttpStatusCodeMapperTests : UnitTest
{
    private readonly ErrorsToHttpStatusCodeMapper _codeMapper = new();

    [Theory]
    [InlineData(OcelotErrorCode.UnauthenticatedError)]
    public void Should_return_unauthorized(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(OcelotErrorCode.CannotFindClaimError)]
    [InlineData(OcelotErrorCode.ClaimValueNotAuthorizedError)]
    [InlineData(OcelotErrorCode.ScopeNotAuthorizedError)]
    [InlineData(OcelotErrorCode.UnauthorizedError)]
    [InlineData(OcelotErrorCode.UserDoesNotHaveClaimError)]
    public void Should_return_forbidden(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(OcelotErrorCode.RequestTimedOutError)]
    public void Should_return_service_unavailable(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(OcelotErrorCode.UnableToCompleteRequestError)]
    [InlineData(OcelotErrorCode.CouldNotFindLoadBalancerCreator)]
    [InlineData(OcelotErrorCode.ErrorInvokingLoadBalancerCreator)]
    public void Should_return_internal_server_error(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(OcelotErrorCode.ConnectionToDownstreamServiceError)]
    public void Should_return_bad_gateway_error(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.BadGateway);
    }

    [Theory]
    [InlineData(OcelotErrorCode.CannotAddDataError)]
    [InlineData(OcelotErrorCode.CannotFindDataError)]
    [InlineData(OcelotErrorCode.DownstreamHostNullOrEmptyError)]
    [InlineData(OcelotErrorCode.DownstreamPathNullOrEmptyError)]
    [InlineData(OcelotErrorCode.DownstreampathTemplateAlreadyUsedError)]
    [InlineData(OcelotErrorCode.DownstreamPathTemplateContainsSchemeError)]
    [InlineData(OcelotErrorCode.DownstreamSchemeNullOrEmptyError)]
    [InlineData(OcelotErrorCode.FileValidationFailedError)]
    [InlineData(OcelotErrorCode.InstructionNotForClaimsError)]
    [InlineData(OcelotErrorCode.NoInstructionsError)]
    [InlineData(OcelotErrorCode.ParsingConfigurationHeaderError)]
    [InlineData(OcelotErrorCode.RateLimitOptionsError)]
    [InlineData(OcelotErrorCode.ServicesAreEmptyError)]
    [InlineData(OcelotErrorCode.ServicesAreNullError)]
    [InlineData(OcelotErrorCode.UnableToCreateAuthenticationHandlerError)]
    [InlineData(OcelotErrorCode.UnableToFindDownstreamRouteError)]
    [InlineData(OcelotErrorCode.UnableToFindLoadBalancerError)]
    [InlineData(OcelotErrorCode.UnableToFindServiceDiscoveryProviderError)]
    [InlineData(OcelotErrorCode.UnableToFindQoSProviderError)]
    [InlineData(OcelotErrorCode.UnknownError)]
    [InlineData(OcelotErrorCode.UnmappableRequestError)]
    [InlineData(OcelotErrorCode.UnsupportedAuthenticationProviderError)]
    public void Should_return_not_found(OcelotErrorCode errorCode)
    {
        ShouldMapErrorToStatusCode(errorCode, HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Bug", "749")] // https://github.com/ThreeMammals/Ocelot/issues/749
    [Trait("PR", "1769")] // https://github.com/ThreeMammals/Ocelot/pull/1769
    public void Should_return_request_entity_too_large()
    {
        ShouldMapErrorsToStatusCode(new() { OcelotErrorCode.PayloadTooLargeError }, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public void AuthenticationErrorsHaveHighestPriority()
    {
        var errors = new List<OcelotErrorCode>
        {
            OcelotErrorCode.CannotAddDataError,
            OcelotErrorCode.CannotFindClaimError,
            OcelotErrorCode.UnauthenticatedError,
            OcelotErrorCode.RequestTimedOutError,
        };

        ShouldMapErrorsToStatusCode(errors, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void AuthorizationErrorsHaveSecondHighestPriority()
    {
        var errors = new List<OcelotErrorCode>
        {
            OcelotErrorCode.CannotAddDataError,
            OcelotErrorCode.CannotFindClaimError,
            OcelotErrorCode.RequestTimedOutError,
        };

        ShouldMapErrorsToStatusCode(errors, HttpStatusCode.Forbidden);
    }

    [Fact]
    public void ServiceUnavailableErrorsHaveThirdHighestPriority()
    {
        var errors = new List<OcelotErrorCode>
        {
            OcelotErrorCode.CannotAddDataError,
            OcelotErrorCode.RequestTimedOutError,
        };

        ShouldMapErrorsToStatusCode(errors, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void Check_we_have_considered_all_errors_in_these_tests()
    {
        // If this test fails then it's because the number of error codes has changed.
        // You should make the appropriate changes to the test cases here to ensure
        // they cover all the error codes, and then modify this assertion.
        Enum.GetNames<OcelotErrorCode>().Length.ShouldBe(42, "Looks like the number of error codes has changed. Do you need to modify ErrorsToHttpStatusCodeMapper?");
    }

    private void ShouldMapErrorToStatusCode(OcelotErrorCode errorCode, HttpStatusCode expectedHttpStatusCode)
    {
        ShouldMapErrorsToStatusCode(new List<OcelotErrorCode> { errorCode }, expectedHttpStatusCode);
    }

    private void ShouldMapErrorsToStatusCode(List<OcelotErrorCode> errorCodes, HttpStatusCode expectedHttpStatusCode)
    {
        // Arrange
        var errors = new List<Error>();
        foreach (var errorCode in errorCodes)
        {
            errors.Add(new AnyError(errorCode));
        }

        // Act
        var result = _codeMapper.Map(errors);

        // Assert
        result.ShouldBe((int)expectedHttpStatusCode);
    }
}

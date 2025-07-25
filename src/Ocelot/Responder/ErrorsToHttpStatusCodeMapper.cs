using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Responder;

public class ErrorsToHttpStatusCodeMapper : IErrorsToHttpStatusCodeMapper
{
    public int Map(List<Error> errors)
    {
        if (errors.Any(e => e.Code == OcelotErrorCode.UnauthenticatedError))
        {
            return 401;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.UnauthorizedError
            || e.Code == OcelotErrorCode.ClaimValueNotAuthorizedError
            || e.Code == OcelotErrorCode.ScopeNotAuthorizedError
            || e.Code == OcelotErrorCode.UserDoesNotHaveClaimError
            || e.Code == OcelotErrorCode.CannotFindClaimError))
        {
            return 403;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.QuotaExceededError))
        {
            return errors.Single(e => e.Code == OcelotErrorCode.QuotaExceededError).HttpStatusCode;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.RequestTimedOutError))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.RequestCanceled))
        {
            // status code refer to
            // https://stackoverflow.com/questions/46234679/what-is-the-correct-http-status-code-for-a-cancelled-request?answertab=votes#tab-top
            // https://httpstatuses.com/499
            return StatusCodes.Status499ClientClosedRequest;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.UnableToFindDownstreamRouteError))
        {
            return 404;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.ConnectionToDownstreamServiceError))
        {
            return 502;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.UnableToCompleteRequestError
            || e.Code == OcelotErrorCode.CouldNotFindLoadBalancerCreator
            || e.Code == OcelotErrorCode.ErrorInvokingLoadBalancerCreator))
        {
            return 500;
        }

        if (errors.Any(e => e.Code == OcelotErrorCode.PayloadTooLargeError))
        {
            return 413;
        }

        return 404;
    }
}

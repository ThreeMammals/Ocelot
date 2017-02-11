using System.Collections.Generic;
using System.Linq;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    public class ErrorsToHttpStatusCodeMapper : IErrorsToHttpStatusCodeMapper
    {
        public Response<int> Map(List<Error> errors)
        {
            if (errors.Any(e => e.Code == OcelotErrorCode.UnauthenticatedError))
            {
                return new OkResponse<int>(401);
            }

            if (errors.Any(e => e.Code == OcelotErrorCode.UnauthorizedError 
                || e.Code == OcelotErrorCode.ClaimValueNotAuthorisedError
                || e.Code == OcelotErrorCode.UserDoesNotHaveClaimError
                || e.Code == OcelotErrorCode.CannotFindClaimError))
            {
                return new OkResponse<int>(403);
            }

            if (errors.Any(e => e.Code == OcelotErrorCode.RequestTimedOutError))
            {
                return new OkResponse<int>(503);
            }

            return new OkResponse<int>(404);
        }
    }
}
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

            return new OkResponse<int>(404);
        }
    }
}
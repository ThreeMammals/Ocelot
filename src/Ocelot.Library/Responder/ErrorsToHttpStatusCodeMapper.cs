namespace Ocelot.Library.Responder
{
    using System.Collections.Generic;
    using System.Linq;
    using Errors;
    using Responses;

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
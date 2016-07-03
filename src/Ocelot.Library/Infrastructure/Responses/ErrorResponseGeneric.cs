using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Responses
{
    public class ErrorResponse<T> : Response<T>
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Responses
{
    public class ErrorResponse : Response
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
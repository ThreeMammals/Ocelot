using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Errors;

namespace Ocelot.Library.Infrastructure.Responses
{
    public class ErrorResponse : Response
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Responses
{
    public class ErrorResponse : Response
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
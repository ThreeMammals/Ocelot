namespace Ocelot.Library.Responses
{
    using System.Collections.Generic;
    using Errors;

    public class ErrorResponse : Response
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
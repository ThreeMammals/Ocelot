namespace Ocelot.Library.Responses
{
    using System.Collections.Generic;
    using Errors;

    public class ErrorResponse<T> : Response<T>
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
namespace Ocelot.ApiGateway.Infrastructure.Responses
{
    using System.Collections.Generic;

    public class ErrorResponse<T> : Response<T>
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
namespace Ocelot.ApiGateway.Infrastructure.Responses
{
    using System.Collections.Generic;

    public class ErrorResponse : Response
    {
        public ErrorResponse(List<Error> errors) : base(errors)
        {
        }
    }
}
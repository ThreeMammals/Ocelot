namespace Ocelot.ApiGateway.Infrastructure.Responses
{
    using System.Collections.Generic;

    public abstract class Response
    {
        protected Response()
        {
            Errors = new List<Error>();
        }

        protected Response(List<Error> errors)
        {
            Errors = errors ?? new List<Error>();
        } 

        public List<Error> Errors { get; private set; }
    }
}
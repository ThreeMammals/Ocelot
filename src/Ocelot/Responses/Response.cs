using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Responses
{
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

        public List<Error> Errors { get; }

        public bool IsError => Errors.Count > 0;
    }
}

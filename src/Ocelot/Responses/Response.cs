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

        public List<Error> Errors { get; private set; }

        public bool IsError
        {
            get
            {
                return Errors.Count > 0;
            }
        }
    }
}
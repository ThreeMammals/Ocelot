using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Responses
{
    public abstract class Response<T> : Response
    {
        protected Response(T data)
        {
            Data = data;
        }

        protected Response(List<Error> errors) : base(errors)
        {
        }

        public T Data { get; private set; }
        
    }
} 
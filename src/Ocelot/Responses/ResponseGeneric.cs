namespace Ocelot.Library.Responses
{
    using System.Collections.Generic;
    using Errors;

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
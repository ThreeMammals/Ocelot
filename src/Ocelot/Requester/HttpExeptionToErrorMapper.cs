namespace Ocelot.Requester
{
    using System;
    using System.Collections.Generic;
    using Errors;
    using Microsoft.Extensions.DependencyInjection;

    public class HttpExeptionToErrorMapper : IExceptionToErrorMapper
    {
        private readonly Dictionary<Type, Func<Exception, Error>> _mappers;

        public HttpExeptionToErrorMapper(IServiceProvider serviceProvider)
        {
            _mappers = serviceProvider.GetService<Dictionary<Type, Func<Exception, Error>>>();
        }

        public Error Map(Exception exception)
        {
            var type = exception.GetType();

            if (_mappers != null && _mappers.ContainsKey(type))
            {
                return _mappers[type](exception);
            }

            return new UnableToCompleteRequestError(exception);
        }
    }
}

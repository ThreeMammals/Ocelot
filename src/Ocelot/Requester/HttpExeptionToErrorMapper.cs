namespace Ocelot.Requester
{
    using Errors;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;

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

            if (type == typeof(OperationCanceledException))
            {
                return new RequestCanceledError(exception.Message);
            }

            return new UnableToCompleteRequestError(exception);
        }
    }
}

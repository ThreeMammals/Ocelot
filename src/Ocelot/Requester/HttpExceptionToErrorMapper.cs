using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Errors.QoS;
using Ocelot.Request.Mapper;

namespace Ocelot.Requester
{
    public class HttpExceptionToErrorMapper : IExceptionToErrorMapper
    {
        /// <summary>This is a dictionary of custom mappers for exceptions.</summary>
        private readonly IDictionary<Type, Func<Exception, Error>> _mappers;

        /// <summary>413 status.</summary>
        private const int RequestEntityTooLarge = (int)HttpStatusCode.RequestEntityTooLarge;

        public HttpExceptionToErrorMapper(IServiceProvider serviceProvider)
        {
            _mappers = serviceProvider.GetService<IDictionary<Type, Func<Exception, Error>>>();
        }

        public Error Map(Exception exception)
        {
            var type = exception.GetType();

            // If there is a custom mapper for this exception type, use it
            // The idea is the following: When implementing features or providers,
            // you can provide a custom mapper
            if (_mappers != null && _mappers.TryGetValue(type, out var mapper))
            {
                return mapper(exception);
            }

            // here are mapped the exceptions thrown from Ocelot core application
            if (type == typeof(TimeoutException))
            {
                return new RequestTimedOutError(exception);
            }

            if (type == typeof(OperationCanceledException) || type.IsSubclassOf(typeof(OperationCanceledException)))
            {
                return new RequestCanceledError(exception.Message);
            }

            if (type == typeof(HttpRequestException) || type == typeof(TimeoutException))
            {
                // Inner exception is a BadHttpRequestException, and only this exception exposes the StatusCode property.
                // We check if the inner exception is a BadHttpRequestException and if the StatusCode is 413, we return a PayloadTooLargeError
                if (exception.InnerException is BadHttpRequestException { StatusCode: RequestEntityTooLarge })
                {
                    return new PayloadTooLargeError(exception);
                }

                return new ConnectionToDownstreamServiceError(exception);
            }

            return new UnableToCompleteRequestError(exception);
        }
    }
}

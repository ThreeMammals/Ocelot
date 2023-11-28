using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;

namespace Ocelot.Requester
{
    public class HttpExceptionToErrorMapper : IExceptionToErrorMapper
    {
        private readonly Dictionary<Type, Func<Exception, Error>> _mappers;

        public HttpExceptionToErrorMapper(IServiceProvider serviceProvider)
        {
            _mappers = serviceProvider.GetService<Dictionary<Type, Func<Exception, Error>>>();
        }

        public Error Map(Exception exception)
        {
            var type = exception.GetType();

            if (_mappers != null && _mappers.TryGetValue(type, out var mapper))
            {
                return mapper(exception);
            }

            if (type == typeof(OperationCanceledException) || type.IsSubclassOf(typeof(OperationCanceledException)))
            {
                return new RequestCanceledError(exception.Message);
            }

            if (type == typeof(HttpRequestException))
            {
                return new ConnectionToDownstreamServiceError(exception);
            }

            return new UnableToCompleteRequestError(exception);
        }
    }
}

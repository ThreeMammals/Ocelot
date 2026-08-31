using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Request.Mapper;

namespace Ocelot.Requester;

public class HttpExceptionToErrorMapper : IExceptionToErrorMapper
{
    /// <summary>This is a dictionary of custom mappers for exceptions.</summary>
    private readonly IDictionary<Type, Func<Exception, Error>> _mappers;

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

        // The exception thrown by Ocelot.Requester.TimeoutDelegatingHandler
        if (type == typeof(TimeoutException) && exception.InnerException is null)
        {
            return new RequestTimedOutError(exception); // -> 504 Gateway Timeout
        }

        if (type == typeof(OperationCanceledException) || type.IsSubclassOf(typeof(OperationCanceledException)))
        {
            return new RequestCanceledError(exception.Message);
        }

        // Bug #2376 -> https://github.com/ThreeMammals/Ocelot/issues/2376
        // Late Catch: Map the HttpRequestException to a 400 Bad Request.
        // If the header format is invalid, HttpClient throws this exception locally.
        // By catching it here, we ensure Ocelot returns a clean 4xx client error instead of a generic 502 Bad Gateway.
        if (exception is HttpRequestException && exception.Message.Contains("Request headers must contain only ASCII characters."))
        {
            return new BadRequestError(exception); // -> 400 Bad Request
        }

        // Map to a 413 Content Too Large (prior Request Entity Too Large) or 502 Bad Gateway
        if (type == typeof(HttpRequestException) || type == typeof(TimeoutException))
        {
            // Inner exception is a BadHttpRequestException, and only this exception exposes the StatusCode property.
            // We check if the inner exception is a BadHttpRequestException and if the StatusCode is 413, we return a PayloadTooLargeError
            if (exception.InnerException is BadHttpRequestException { StatusCode: StatusCodes.Status413RequestEntityTooLarge })
            {
                return new PayloadTooLargeError(exception); // -> 413 Content Too Large
            }

            return new ConnectionToDownstreamServiceError(exception); // -> 502 Bad Gateway
        }

        return new UnableToCompleteRequestError(exception); // -> 500 Internal Server Error
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Request.Mapper;

namespace Ocelot.Requester;

public class HttpExceptionToErrorMapper : IExceptionToErrorMapper
{
    private readonly IDictionary<Type, Func<Exception, Error>> _mappers;
    private readonly IEnumerable<IExceptionMapper> _handlers;

    public HttpExceptionToErrorMapper(IServiceProvider serviceProvider, IEnumerable<IExceptionMapper> handlers)
    {
        _mappers = serviceProvider.GetService<IDictionary<Type, Func<Exception, Error>>>();
        _handlers = handlers;
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
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(exception))
            {
                return handler.Map(exception);
            }
        }

        return new UnableToCompleteRequestError(exception);
    }
}

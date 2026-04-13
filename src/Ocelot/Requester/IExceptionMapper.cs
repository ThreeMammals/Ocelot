using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Request.Mapper;

namespace Ocelot.Requester;

public interface IExceptionMapper
{
    bool CanHandle(Exception ex);
    Error Map(Exception ex);
}

public class TimeoutExceptionMapper : IExceptionMapper
{
    public bool CanHandle(Exception ex) => ex is TimeoutException;
    public Error Map(Exception ex) => new RequestTimedOutError(ex);
}

public class OperationCanceledExceptionMapper : IExceptionMapper
{
    public bool CanHandle(Exception ex) => ex is OperationCanceledException;
    public Error Map(Exception ex) => new RequestCanceledError(ex.Message);
}

public class BadHttpRequestExceptionMapper : IExceptionMapper
{
    public bool CanHandle(Exception ex) => ex is BadHttpRequestException;

    public Error Map(Exception ex)
    {
        var badHttpRequestException = (BadHttpRequestException)ex;
        if (badHttpRequestException.StatusCode == StatusCodes.Status413RequestEntityTooLarge)
        {
            return new PayloadTooLargeError(ex);
        }

        if (badHttpRequestException.StatusCode == StatusCodes.Status400BadRequest)
        {
            return new InvalidRequestError(ex);
        }

        return new UnableToCompleteRequestError(ex);
    }
}

public class HttpRequestExceptionMapper : IExceptionMapper
{
    public bool CanHandle(Exception ex) => ex is HttpRequestException;

    public Error Map(Exception ex)
    {
        if (ex.InnerException is BadHttpRequestException inner)
        {
            if (inner.StatusCode == StatusCodes.Status413RequestEntityTooLarge)
            {
                return new PayloadTooLargeError(ex);
            }

            if (inner.StatusCode == StatusCodes.Status400BadRequest)
            {
                return new InvalidRequestError(ex);
            }
        }

        if (ex.Message.Contains("Request headers must contain only ASCII characters"))
        {
            return new InvalidRequestError(ex);
        }

        return new ConnectionToDownstreamServiceError(ex);
    }
}

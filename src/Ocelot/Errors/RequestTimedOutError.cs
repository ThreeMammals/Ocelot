using Microsoft.AspNetCore.Http;

namespace Ocelot.Errors;

public class RequestTimedOutError : Error
{
    public RequestTimedOutError(Exception exception)
        : base($"Timeout making http request, exception: {exception}",
            OcelotErrorCode.RequestTimedOutError, StatusCodes.Status503ServiceUnavailable)
    { }
}

using StatusCode = System.Net.HttpStatusCode;

namespace Ocelot.Errors.QoS;

public class RequestTimedOutError : Error
{
    public RequestTimedOutError(Exception exception)
        : base($"Timeout making http request, exception: {exception}", OcelotErrorCode.RequestTimedOutError, (int)StatusCode.ServiceUnavailable)
    {
    }
}

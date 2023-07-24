using Ocelot.Errors;
using System;

namespace Ocelot.Provider.Polly
{
    public class RequestTimedOutError : Error
    {
        public RequestTimedOutError(Exception exception)
            : base($"Timeout making http request, exception: {exception}", OcelotErrorCode.RequestTimedOutError, 503)
        {
        }
    }
}

namespace Ocelot.Provider.Polly
{
    using Errors;
    using System;

    public class RequestTimedOutError : Error
    {
        public RequestTimedOutError(Exception exception)
            : base($"Timeout making http request, exception: {exception}", OcelotErrorCode.RequestTimedOutError)
        {
        }
    }
}

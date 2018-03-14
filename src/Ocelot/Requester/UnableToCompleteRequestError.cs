using System;
using Ocelot.Errors;

namespace Ocelot.Requester
{
    public class UnableToCompleteRequestError : Error
    {
        public UnableToCompleteRequestError(Exception exception) 
            : base($"Error making http request, exception: {exception}", OcelotErrorCode.UnableToCompleteRequestError)
        {
        }
    }
}

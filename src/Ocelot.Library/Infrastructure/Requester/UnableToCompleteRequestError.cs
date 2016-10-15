using System;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Requester
{
    public class UnableToCompleteRequestError : Error
    {
        public UnableToCompleteRequestError(Exception exception) 
            : base($"Error making http request, exception: {exception.Message}", OcelotErrorCode.UnableToCompleteRequestError)
        {
        }
    }
}

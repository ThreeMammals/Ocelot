namespace Ocelot.Library.Requester
{
    using System;
    using Errors;

    public class UnableToCompleteRequestError : Error
    {
        public UnableToCompleteRequestError(Exception exception) 
            : base($"Error making http request, exception: {exception.Message}", OcelotErrorCode.UnableToCompleteRequestError)
        {
        }
    }
}

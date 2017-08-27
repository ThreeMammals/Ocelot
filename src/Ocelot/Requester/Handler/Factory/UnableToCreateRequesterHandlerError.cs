using Ocelot.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Requester.Handler.Factory
{
    public class UnableToCreateRequesterHandlerError : Error
    {
        public UnableToCreateRequesterHandlerError(string message) 
            : base(message, OcelotErrorCode.UnableToCreateRequesterHandlerError)
        {
        }
    }
}

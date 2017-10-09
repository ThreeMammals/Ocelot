using Ocelot.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Request.Builder.Factory
{
    public class UnableToCreateRequestBuilderError : Error
    {
        public UnableToCreateRequestBuilderError(string message) 
            : base(message, OcelotErrorCode.UnableToCreateRequestBuilderError)
        {
        }
    }
}

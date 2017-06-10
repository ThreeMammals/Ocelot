using Ocelot.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Validator
{
    public class RateLimitOptionsValidationError : Error
    {
        public RateLimitOptionsValidationError(string message) 
            : base(message, OcelotErrorCode.RateLimitOptionsError)
        {
        }
    }
}

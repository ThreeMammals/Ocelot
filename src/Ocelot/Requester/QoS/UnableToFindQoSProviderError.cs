using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Errors;

namespace Ocelot.Requester.QoS
{
    public class UnableToFindQoSProviderError : Error
    {
        public UnableToFindQoSProviderError(string message) 
            : base(message, OcelotErrorCode.UnableToFindQoSProviderError)
        {
        }
    }
}

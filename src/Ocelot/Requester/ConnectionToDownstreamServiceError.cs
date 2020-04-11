using Ocelot.Errors;
using System;

namespace Ocelot.Requester
{
    class ConnectionToDownstreamServiceError : Error
    {
        public ConnectionToDownstreamServiceError(Exception exception)
            : base($"Error connecting to downstream service, exception: {exception}", OcelotErrorCode.ConnectionToDownstreamServiceError)
        {
        }
    }
}

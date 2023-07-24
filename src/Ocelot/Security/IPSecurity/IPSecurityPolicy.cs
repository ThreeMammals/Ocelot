using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Security.IPSecurity
{
    public class IPSecurityPolicy : ISecurityPolicy
    {
        public async Task<Response> Security(DownstreamRoute downstreamRoute, HttpContext httpContext)
        {
            var clientIp = httpContext.Connection.RemoteIpAddress;
            var securityOptions = downstreamRoute.SecurityOptions;
            if (securityOptions == null)
            {
                return new OkResponse();
            }

            if (securityOptions.IPBlockedList != null)
            {
                if (securityOptions.IPBlockedList.Exists(f => f == clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($" This request rejects access to {clientIp} IP");
                    return new ErrorResponse(error);
                }
            }

            if (securityOptions.IPAllowedList?.Count > 0)
            {
                if (!securityOptions.IPAllowedList.Exists(f => f == clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($"{clientIp} does not allow access, the request is invalid");
                    return new ErrorResponse(error);
                }
            }

            return await Task.FromResult(new OkResponse());
        }
    }
}

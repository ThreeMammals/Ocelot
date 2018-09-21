using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Security.IPSecurity
{
    public class IPSecurityPolicy : ISecurityPolicy
    {
        public async Task<Response> Security(DownstreamContext context)
        {
            IPAddress clientIp = context.HttpContext.Connection.RemoteIpAddress;
            SecurityOptions securityOptions = context.DownstreamReRoute.SecurityOptions;
            if (securityOptions == null)
            {
                return new OkResponse();
            }
            if (securityOptions.IPBlacklist != null)
            {
                if (securityOptions.IPBlacklist.Exists(f => f == clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($"{clientIp.ToString()} Cannot request to enter the blacklist");
                    return new ErrorResponse(error);
                }
            }
            if (securityOptions.IPWhitelist != null)
            {
                if (!securityOptions.IPWhitelist.Exists(f => f == clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($"{clientIp.ToString()}  is not in the whitelist, the request is invalid");
                    return new ErrorResponse(error);
                }
            }
            return await Task.FromResult(new OkResponse());
        }
    }
}

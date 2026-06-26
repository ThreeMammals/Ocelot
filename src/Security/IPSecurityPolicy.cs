using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Security;

public class IPSecurityPolicy : ISecurityPolicy
{
    public Response Security(DownstreamRoute downstreamRoute, HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress;
        var options = downstreamRoute.SecurityOptions;
        if (options == null || clientIp == null)
        {
            return new OkResponse();
        }

        if (options.IPBlockedList?.Count > 0)
        {
            if (options.IPBlockedList.Contains(clientIp.ToString()))
            {
                var error = new IpSecurityError($"Access denied: client IP {clientIp} is blocked."); // authentication or not you shall not pass! <:^) 
                return new ErrorResponse(error);
            }
        }

        if (options.IPAllowedList?.Count > 0)
        {
            if (!options.IPAllowedList.Contains(clientIp.ToString()))
            {
                var error = new IpSecurityError($"Access denied: client IP {clientIp} is not in the allow list.");
                return new ErrorResponse(error);
            }
        }

        return new OkResponse();
    }

    public Task<Response> SecurityAsync(DownstreamRoute downstreamRoute, HttpContext context)
        => Task.Run(() => Security(downstreamRoute, context), context.RequestAborted);
}

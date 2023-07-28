using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Threading.Tasks;

namespace Ocelot.Security.IPSecurity;

public class IPSecurityPolicy : ISecurityPolicy
{
    public Response Security(DownstreamRoute downstreamRoute, HttpContext context)
    {
        var clientIp = context.GetClientIpAddress();
        var options = downstreamRoute.SecurityOptions;
        if (options == null || clientIp == null)
        {
            return new OkResponse();
        }

        if (options.IPBlockedList?.Count > 0)
        {
            if (options.IPBlockedList.Contains(clientIp))
            {
                var error = new UnauthenticatedError($"This request rejects access to {clientIp} IP");
                return new ErrorResponse(error);
            }
        }

        if (options.IPAllowedList?.Count > 0)
        {
            if (!options.IPAllowedList.Contains(clientIp))
            {
                var error = new UnauthenticatedError($"{clientIp} does not allow access, the request is invalid");
                return new ErrorResponse(error);
            }
        }

        return new OkResponse();
    }

    public Task<Response> SecurityAsync(DownstreamRoute downstreamRoute, HttpContext context)
        => Task.Run(() => Security(downstreamRoute, context));
}

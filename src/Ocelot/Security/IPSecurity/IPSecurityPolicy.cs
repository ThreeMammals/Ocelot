using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Security.IPSecurity;

public class IPSecurityPolicy : ISecurityPolicy
{
    public ValueTask<Response> SecurityAsync(DownstreamRoute downstreamRoute, HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress;
        var options = downstreamRoute.SecurityOptions;
        if (options == null || clientIp == null)
        {
            return ValueTask.FromResult(OkResponse);
        }

        if (options.IPBlockedList?.Count > 0)
        {
            if (options.IPBlockedList.Contains(clientIp.ToString()))
            {
                var error = new UnauthenticatedError($"This request rejects access to {clientIp} IP");
                return ValueTask.FromResult<Response>(new ErrorResponse(error));
            }
        }

        if (options.IPAllowedList?.Count > 0)
        {
            if (!options.IPAllowedList.Contains(clientIp.ToString()))
            {
                var error = new UnauthenticatedError($"{clientIp} does not allow access, the request is invalid");
                return ValueTask.FromResult<Response>(new ErrorResponse(error));
            }
        }

        return ValueTask.FromResult(OkResponse);
    }

    private static Response OkResponse { get; } = new OkResponse();
}

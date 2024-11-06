﻿using Microsoft.AspNetCore.Http;
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

            if (securityOptions.IPBlockedList != null && securityOptions.IPBlockedList.Count > 0 && clientIp != null)
            {
                if (securityOptions.IPBlockedList.Contains(clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($" This request rejects access to {clientIp} IP");
                    return new ErrorResponse(error);
                }
            }

            if (securityOptions.IPAllowedList != null && securityOptions.IPAllowedList.Count > 0 && clientIp != null)
            {
                if (!securityOptions.IPAllowedList.Contains(clientIp.ToString()))
                {
                    var error = new UnauthenticatedError($"{clientIp} does not allow access, the request is invalid");
                    return new ErrorResponse(error);
                }
            }

            return await Task.FromResult(new OkResponse());
        }
    }
}

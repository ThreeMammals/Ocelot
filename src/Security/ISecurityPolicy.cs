using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Security;

public interface ISecurityPolicy
{
    Response Security(DownstreamRoute downstreamRoute, HttpContext context);
    Task<Response> SecurityAsync(DownstreamRoute downstreamRoute, HttpContext context);
}

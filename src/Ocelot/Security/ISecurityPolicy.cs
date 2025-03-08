using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Security;

public interface ISecurityPolicy
{
    ValueTask<Response> SecurityAsync(DownstreamRoute downstreamRoute, HttpContext context);
}

using System.Threading.Tasks;

using Ocelot.Configuration;

using Microsoft.AspNetCore.Http;

using Ocelot.Responses;

namespace Ocelot.Security
{
    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamRoute downstreamRoute, HttpContext httpContext);
    }
}

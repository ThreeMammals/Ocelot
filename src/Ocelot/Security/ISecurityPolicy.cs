using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;
using System.Threading.Tasks;

namespace Ocelot.Security
{
    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamRoute downstreamRoute, HttpContext httpContext);
    }
}

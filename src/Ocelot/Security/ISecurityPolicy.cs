namespace Ocelot.Security
{
    using System.Threading.Tasks;

    using Configuration;

    using Microsoft.AspNetCore.Http;

    using Responses;

    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamRoute downstreamRoute, HttpContext httpContext);
    }
}

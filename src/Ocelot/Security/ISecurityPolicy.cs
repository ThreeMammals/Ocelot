namespace Ocelot.Security
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Configuration;
    using Responses;

    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamRoute downstreamRoute, HttpContext httpContext);
    }
}

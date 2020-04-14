namespace Ocelot.Security
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System.Threading.Tasks;

    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamContext downstreamContext, HttpContext httpContext);
    }
}

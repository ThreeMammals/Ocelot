namespace Ocelot.Middleware.Multiplexer
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration;
    using System.Threading.Tasks;

    public interface IMultiplexer
    {
        Task Multiplex(IDownstreamContext context, HttpContext httpContext, ReRoute reRoute, RequestDelegate next);
    }
}

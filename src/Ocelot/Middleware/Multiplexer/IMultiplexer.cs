namespace Ocelot.Middleware.Multiplexer
{
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    public interface IMultiplexer
    {
        Task Multiplex(IDownstreamContext context, HttpContext httpContext, RequestDelegate next);
    }
}

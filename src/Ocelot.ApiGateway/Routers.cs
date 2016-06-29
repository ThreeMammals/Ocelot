using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ocelot.ApiGateway
{
    public class Router : IRouter
    {
        public Task RouteAsync(RouteContext context)
        {
            context.Handler = async c =>
            {
                await c.Response.WriteAsync($"Hi, Tom!");
            };
            
            return Task.FromResult(0);
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }
    }
}
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;

namespace Ocelot.Middleware
{
    public delegate Task OcelotMiddlewareConfigurationDelegate(IApplicationBuilder builder);
}

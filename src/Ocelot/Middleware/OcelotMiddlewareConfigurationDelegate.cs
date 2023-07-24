using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;

namespace Ocelot.Middleware
{
    public delegate Task OcelotMiddlewareConfigurationDelegate(IApplicationBuilder builder);
}

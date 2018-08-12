namespace Ocelot.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;

    public delegate Task OcelotMiddlewareConfigurationDelegate(IApplicationBuilder builder);
}

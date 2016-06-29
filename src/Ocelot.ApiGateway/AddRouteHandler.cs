using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.ApiGateway
{
    public static class HelloExtensions
    {
        public static IRouteBuilder AddRouter(this IRouteBuilder routeBuilder,
            IApplicationBuilder app)
        {
            routeBuilder.Routes.Add(new Route(new Router(),
                "{*url}", 
                app.ApplicationServices.GetService<IInlineConstraintResolver>()));

            return routeBuilder;
        }
    } 
}
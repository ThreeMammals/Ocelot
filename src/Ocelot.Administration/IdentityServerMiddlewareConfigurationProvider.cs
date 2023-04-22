namespace Ocelot.Administration
{
    using System.Threading.Tasks;

    using Configuration.Repository;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using Middleware;

    public static class IdentityServerMiddlewareConfigurationProvider
    {
        public static OcelotMiddlewareConfigurationDelegate Get = builder =>
        {
            var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();

            var config = internalConfigRepo.Get();

            if (!string.IsNullOrEmpty(config.Data.AdministrationPath))
            {
                builder.Map(config.Data.AdministrationPath, app =>
                {
                    //todo - hack so we know that we are using internal identity server
                    var identityServerConfiguration = builder.ApplicationServices.GetService<IIdentityServerConfiguration>();

                    if (identityServerConfiguration != null)
                    {
                        app.UseIdentityServer();
                    }

                    app.UseAuthentication();
                    app.UseRouting();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapDefaultControllerRoute();
                        endpoints.MapControllers();
                    });
                });
            }

            return Task.CompletedTask;
        };
    }
}

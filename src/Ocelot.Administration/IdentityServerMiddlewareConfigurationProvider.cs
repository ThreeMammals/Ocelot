﻿namespace Ocelot.Administration
{
    using System.Threading.Tasks;
    using Configuration;
    using Configuration.Repository;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Middleware;

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
                    app.UseMvc();
                });
            }

            return Task.CompletedTask;
        };
    }
}

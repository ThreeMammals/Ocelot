﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Authentication.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Claims.Middleware;
using Ocelot.Controllers;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.Logging;
using Ocelot.QueryStrings.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using Ocelot.RateLimit.Middleware;

namespace Ocelot.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Authorisation.Middleware;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Provider;
    using Ocelot.Configuration.Repository;
    using Ocelot.Configuration.Setter;
    using Ocelot.LoadBalancer.Middleware;
    using Ocelot.Responses;

    public static class OcelotMiddlewareExtensions
    {
        /// <summary>
        /// Registers the Ocelot default middlewares
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder)
        {
            await builder.UseOcelot(new OcelotMiddlewareConfiguration());

            return builder;
        }

        /// <summary>
        /// Registers Ocelot with a combination of default middlewares and optional middlewares in the configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="middlewareConfiguration"></param>
        /// <returns></returns>
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, OcelotMiddlewareConfiguration middlewareConfiguration)
        {
            var configuration = await CreateConfiguration(builder);
            
            await CreateAdministrationArea(builder, configuration);

            ConfigureDiagnosticListener(builder);

            // This is registered to catch any global exceptions that are not handled
            builder.UseExceptionHandlerMiddleware();

            // Allow the user to respond with absolutely anything they want.
            builder.UseIfNotNull(middlewareConfiguration.PreErrorResponderMiddleware);

            // This is registered first so it can catch any errors and issue an appropriate response
            builder.UseResponderMiddleware();

            // Initialises downstream request
            builder.UseDownstreamRequestInitialiser();

            // Then we get the downstream route information
            builder.UseDownstreamRouteFinderMiddleware();

            // We check whether the request is ratelimit, and if there is no continue processing
            builder.UseRateLimiting();

            // Now we can look for the requestId
            builder.UseRequestIdMiddleware();

            // Allow pre authentication logic. The idea being people might want to run something custom before what is built in.
            builder.UseIfNotNull(middlewareConfiguration.PreAuthenticationMiddleware);

            // Now we know where the client is going to go we can authenticate them.
            // We allow the ocelot middleware to be overriden by whatever the
            // user wants
            if (middlewareConfiguration.AuthenticationMiddleware == null)
            {
                builder.UseAuthenticationMiddleware();
            }
            else
            {
                builder.Use(middlewareConfiguration.AuthenticationMiddleware);
            }

            // The next thing we do is look at any claims transforms in case this is important for authorisation
            builder.UseClaimsBuilderMiddleware();

            // Allow pre authorisation logic. The idea being people might want to run something custom before what is built in.
            builder.UseIfNotNull(middlewareConfiguration.PreAuthorisationMiddleware);

            // Now we have authenticated and done any claims transformation we 
            // can authorise the request
            // We allow the ocelot middleware to be overriden by whatever the
            // user wants
            if (middlewareConfiguration.AuthorisationMiddleware == null)
            {
                builder.UseAuthorisationMiddleware();
            }
            else
            {
                builder.Use(middlewareConfiguration.AuthorisationMiddleware);
            }

            // Now we can run any header transformation logic
            builder.UseHttpRequestHeadersBuilderMiddleware();

            // Allow the user to implement their own query string manipulation logic
            builder.UseIfNotNull(middlewareConfiguration.PreQueryStringBuilderMiddleware);

            // Now we can run any query string transformation logic
            builder.UseQueryStringBuilderMiddleware();

            // Get the load balancer for this request
            builder.UseLoadBalancingMiddleware();

            // This takes the downstream route we retrieved earlier and replaces any placeholders with the variables that should be used
            builder.UseDownstreamUrlCreatorMiddleware();

            // Not sure if this is the best place for this but we use the downstream url 
            // as the basis for our cache key.
            builder.UseOutputCacheMiddleware();

            // Everything should now be ready to build or HttpRequest
            builder.UseHttpRequestBuilderMiddleware();

            //We fire off the request and set the response on the scoped data repo
            builder.UseHttpRequesterMiddleware();

            return builder;
        }

        private static async Task<IOcelotConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            var fileConfig = (IOptions<FileConfiguration>)builder.ApplicationServices.GetService(typeof(IOptions<FileConfiguration>));
            
            var configSetter = (IFileConfigurationSetter)builder.ApplicationServices.GetService(typeof(IFileConfigurationSetter));
            
            var configProvider = (IOcelotConfigurationProvider)builder.ApplicationServices.GetService(typeof(IOcelotConfigurationProvider));

            var ocelotConfiguration = await configProvider.Get();

            if (ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError)
            {
                Response config = null;
                var fileConfigRepo = builder.ApplicationServices.GetService(typeof(IFileConfigurationRepository));
                if (fileConfigRepo.GetType() == typeof(ConsulFileConfigurationRepository))
                {
                    var consulFileConfigRepo = (ConsulFileConfigurationRepository) fileConfigRepo;
                    var ocelotConfigurationRepository =
                        (IOcelotConfigurationRepository) builder.ApplicationServices.GetService(
                            typeof(IOcelotConfigurationRepository));
                    var ocelotConfigurationCreator =
                        (IOcelotConfigurationCreator) builder.ApplicationServices.GetService(
                            typeof(IOcelotConfigurationCreator));

                    var fileConfigFromConsul = await consulFileConfigRepo.Get();
                    if (fileConfigFromConsul.Data == null)
                    {
                        config = await configSetter.Set(fileConfig.Value);
                    }
                    else
                    {
                        var ocelotConfig = await ocelotConfigurationCreator.Create(fileConfigFromConsul.Data);
                        config = await ocelotConfigurationRepository.AddOrReplace(ocelotConfig.Data);
                        var hack = builder.ApplicationServices.GetService(typeof(ConsulFileConfigurationPoller));
                    }
                }
                else
                {
                    config = await configSetter.Set(fileConfig.Value);
                }

                if (config == null || config.IsError)
                {
                    throw new Exception("Unable to start Ocelot: configuration was not set up correctly.");
                }
            }

            ocelotConfiguration = await configProvider.Get();

            if(ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError)
            {
                throw new Exception("Unable to start Ocelot: ocelot configuration was not returned by provider.");
            }

            return ocelotConfiguration.Data;
        }

        private static async Task CreateAdministrationArea(IApplicationBuilder builder, IOcelotConfiguration configuration)
        {
            var identityServerConfiguration = (IIdentityServerConfiguration)builder.ApplicationServices.GetService(typeof(IIdentityServerConfiguration));

            if(!string.IsNullOrEmpty(configuration.AdministrationPath) && identityServerConfiguration != null)
            {
                builder.Map(configuration.AdministrationPath, app =>
                {
                    app.UseIdentityServer();
                    app.UseAuthentication();
                    app.UseMvc();
                });
            }
        }
        
        private static void UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        {
            if (middleware != null)
            {
                builder.Use(middleware);
            }
        }

        /// <summary>
         /// Configure a DiagnosticListener to listen for diagnostic events when the middleware starts and ends
         /// </summary>
         /// <param name="builder"></param>
         private static void ConfigureDiagnosticListener(IApplicationBuilder builder)
         {
             var env = (IHostingEnvironment)builder.ApplicationServices.GetService(typeof(IHostingEnvironment));

            //https://github.com/TomPallister/Ocelot/pull/87 not sure why only for dev envs and marc disapeered so just merging and maybe change one day?
            if (!env.IsProduction())
             {
                 var listener = (OcelotDiagnosticListener)builder.ApplicationServices.GetService(typeof(OcelotDiagnosticListener));
                 var diagnosticListener = (DiagnosticListener)builder.ApplicationServices.GetService(typeof(DiagnosticListener));
                 diagnosticListener.SubscribeWithAdapter(listener);
             }
         }
    }
}

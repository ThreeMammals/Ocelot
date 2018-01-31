using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Authentication.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Claims.Middleware;
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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Authorisation.Middleware;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Provider;
    using Ocelot.Configuration.Repository;
    using Ocelot.Configuration.Setter;
    using Ocelot.LoadBalancer.Middleware;
    using Ocelot.Raft;
    using Ocelot.Responses;
    using Rafty.Concensus;
    using Rafty.Infrastructure;

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

            if(UsingRafty(builder))
            {
                SetUpRafty(builder);
            }

            ConfigureDiagnosticListener(builder);

            // This is registered to catch any global exceptions that are not handled
            // It also sets the Request Id if anything is set globally
            builder.UseExceptionHandlerMiddleware();

            // Allow the user to respond with absolutely anything they want.
            builder.UseIfNotNull(middlewareConfiguration.PreErrorResponderMiddleware);

            // This is registered first so it can catch any errors and issue an appropriate response
            builder.UseResponderMiddleware();

            // Then we get the downstream route information
            builder.UseDownstreamRouteFinderMiddleware();

            // Now we have the ds route we can transform headers and stuff?
            builder.UseHttpHeadersTransformationMiddleware();

            // Initialises downstream request
            builder.UseDownstreamRequestInitialiser();

            // We check whether the request is ratelimit, and if there is no continue processing
            builder.UseRateLimiting();

            // This adds or updates the request id (initally we try and set this based on global config in the error handling middleware)
            // If anything was set at global level and we have a different setting at re route level the global stuff will be overwritten
            // This means you can get a scenario where you have a different request id from the first piece of middleware to the request id middleware.
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

        private static bool UsingRafty(IApplicationBuilder builder)
        {
            var possible = builder.ApplicationServices.GetService(typeof(INode)) as INode;
            if(possible != null)
            {
                return true;
            }

            return false;
        }

        private static void SetUpRafty(IApplicationBuilder builder)
        {
            var applicationLifetime = (IApplicationLifetime)builder.ApplicationServices.GetService(typeof(IApplicationLifetime));
            applicationLifetime.ApplicationStopping.Register(() => OnShutdown(builder));
            var node = (INode)builder.ApplicationServices.GetService(typeof(INode));
            var nodeId = (NodeId)builder.ApplicationServices.GetService(typeof(NodeId));
            node.Start(nodeId.Id);
        }

        private static async Task<IOcelotConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            var deps = GetDependencies(builder);

            var ocelotConfiguration = await deps.provider.Get();

            if (ConfigurationNotSetUp(ocelotConfiguration))
            {
                var response = await SetConfig(builder, deps.fileConfiguration, deps.setter, deps.provider, deps.repo);
                
                if (UnableToSetConfig(response))
                {
                    ThrowToStopOcelotStarting(response);
                }
            }

            return await GetOcelotConfigAndReturn(deps.provider);
        }

        private static async Task<Response> SetConfig(IApplicationBuilder builder, IOptions<FileConfiguration> fileConfiguration, IFileConfigurationSetter setter, IOcelotConfigurationProvider provider, IFileConfigurationRepository repo)
        {
            if (UsingConsul(repo))
            {
                return await SetUpConfigFromConsul(builder, repo, setter, fileConfiguration);
            }
            
            return await setter.Set(fileConfiguration.Value);
        }

        private static bool UnableToSetConfig(Response response)
        {
            return response == null || response.IsError;
        }

        private static bool ConfigurationNotSetUp(Ocelot.Responses.Response<IOcelotConfiguration> ocelotConfiguration)
        {
            return ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError;
        }

        private static (IOptions<FileConfiguration> fileConfiguration, IFileConfigurationSetter setter, IOcelotConfigurationProvider provider, IFileConfigurationRepository repo) GetDependencies(IApplicationBuilder builder)
        {
            var fileConfiguration = (IOptions<FileConfiguration>)builder.ApplicationServices.GetService(typeof(IOptions<FileConfiguration>));
            
            var setter = (IFileConfigurationSetter)builder.ApplicationServices.GetService(typeof(IFileConfigurationSetter));
            
            var provider = (IOcelotConfigurationProvider)builder.ApplicationServices.GetService(typeof(IOcelotConfigurationProvider));

            var repo = (IFileConfigurationRepository)builder.ApplicationServices.GetService(typeof(IFileConfigurationRepository));

            return (fileConfiguration, setter, provider, repo);
        }

        private static async Task<IOcelotConfiguration> GetOcelotConfigAndReturn(IOcelotConfigurationProvider provider)
        {
            var ocelotConfiguration = await provider.Get();

            if(ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError)
            {
                ThrowToStopOcelotStarting(ocelotConfiguration);
            }

            return ocelotConfiguration.Data;
        }

        private static void ThrowToStopOcelotStarting(Response config)
        {
            throw new Exception($"Unable to start Ocelot, errors are: {string.Join(",", config.Errors.Select(x => x.ToString()))}");
        }

        private static bool UsingConsul(IFileConfigurationRepository fileConfigRepo)
        {
            return fileConfigRepo.GetType() == typeof(ConsulFileConfigurationRepository);
        }

        private static async Task<Response> SetUpConfigFromConsul(IApplicationBuilder builder, IFileConfigurationRepository consulFileConfigRepo, IFileConfigurationSetter setter, IOptions<FileConfiguration> fileConfig)
        {
            Response config = null;

            var ocelotConfigurationRepository =
                (IOcelotConfigurationRepository) builder.ApplicationServices.GetService(
                    typeof(IOcelotConfigurationRepository));
            var ocelotConfigurationCreator =
                (IOcelotConfigurationCreator) builder.ApplicationServices.GetService(
                    typeof(IOcelotConfigurationCreator));

            var fileConfigFromConsul = await consulFileConfigRepo.Get();
            if (fileConfigFromConsul.Data == null)
            {
                config = await setter.Set(fileConfig.Value);
            }
            else
            {
                var ocelotConfig = await ocelotConfigurationCreator.Create(fileConfigFromConsul.Data);
                if(ocelotConfig.IsError)
                {
                    return new ErrorResponse(ocelotConfig.Errors);
                }
                config = await ocelotConfigurationRepository.AddOrReplace(ocelotConfig.Data);
                //todo - this starts the poller if it has been registered...please this is so bad.
                var hack = builder.ApplicationServices.GetService(typeof(ConsulFileConfigurationPoller));
            }

            return new OkResponse();
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
            var listener = (OcelotDiagnosticListener)builder.ApplicationServices.GetService(typeof(OcelotDiagnosticListener));
            var diagnosticListener = (DiagnosticListener)builder.ApplicationServices.GetService(typeof(DiagnosticListener));
            diagnosticListener.SubscribeWithAdapter(listener);
         }
        
        private static void OnShutdown(IApplicationBuilder app)
        {
            var node = (INode)app.ApplicationServices.GetService(typeof(INode));
            node.Stop();
        }
    }
}

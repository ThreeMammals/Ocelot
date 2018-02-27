namespace Ocelot.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Provider;
    using Ocelot.Configuration.Repository;
    using Ocelot.Configuration.Setter;
    using Ocelot.Responses;
    using Ocelot.Logging;
    using Rafty.Concensus;
    using Rafty.Infrastructure;
    using Ocelot.Middleware.Pipeline;

    public static class OcelotMiddlewareExtensions
    {
        /// <summary>
        /// Registers the Ocelot default middlewares
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder)
        {
            await builder.UseOcelot(new OcelotPipelineConfiguration());

            return builder;
        }

        /// <summary>
        /// Registers Ocelot with a combination of default middlewares and optional middlewares in the configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="pipelineConfiguration"></param>
        /// <returns></returns>
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            var configuration = await CreateConfiguration(builder);
            
            CreateAdministrationArea(builder, configuration);

            if(UsingRafty(builder))
            {
                SetUpRafty(builder);
            }

            ConfigureDiagnosticListener(builder);

            var pipelineBuilder = new OcelotPipelineBuilder(builder.ApplicationServices);

            pipelineBuilder.BuildOcelotPipeline(pipelineConfiguration);

            var firstDelegate = pipelineBuilder.Build();

            //inject first delegate into first piece of asp.net middleware..maybe not like this
            //then because we are updating the http context in ocelot it comes out correct for
            //rest of asp.net..

            builder.Use(async (context, task) =>
            {
                var downstreamContext = new DownstreamContext(context);
                await firstDelegate.Invoke(downstreamContext);
            });

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

        private static void CreateAdministrationArea(IApplicationBuilder builder, IOcelotConfiguration configuration)
        {
            if(!string.IsNullOrEmpty(configuration.AdministrationPath))
            {
                builder.Map(configuration.AdministrationPath, app =>
                {
                    //todo - hack so we know that we are using internal identity server
                    var identityServerConfiguration = (IIdentityServerConfiguration)builder.ApplicationServices.GetService(typeof(IIdentityServerConfiguration));
                    if (identityServerConfiguration != null)
                    {
                        app.UseIdentityServer();
                    }

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

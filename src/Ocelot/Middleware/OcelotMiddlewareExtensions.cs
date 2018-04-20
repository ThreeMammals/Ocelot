namespace Ocelot.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Repository;
    using Ocelot.Configuration.Setter;
    using Ocelot.Responses;
    using Ocelot.Logging;
    using Rafty.Concensus;
    using Rafty.Infrastructure;
    using Ocelot.Middleware.Pipeline;
    using Pivotal.Discovery.Client;

    public static class OcelotMiddlewareExtensions
    {
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder)
        {
            await builder.UseOcelot(new OcelotPipelineConfiguration());

            return builder;
        }

        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            var configuration = await CreateConfiguration(builder);
            
            CreateAdministrationArea(builder, configuration);

            if(UsingRafty(builder))
            {
                SetUpRafty(builder);
            }

            if (UsingEurekaServiceDiscoveryProvider(configuration))
            {
                builder.UseDiscoveryClient();
            }

            ConfigureDiagnosticListener(builder);

            var pipelineBuilder = new OcelotPipelineBuilder(builder.ApplicationServices);

            pipelineBuilder.BuildOcelotPipeline(pipelineConfiguration);

            var firstDelegate = pipelineBuilder.Build();

            /*
            inject first delegate into first piece of asp.net middleware..maybe not like this
            then because we are updating the http context in ocelot it comes out correct for
            rest of asp.net..
            */

            builder.Properties["analysis.NextMiddlewareName"] = "TransitionToOcelotMiddleware";

            builder.Use(async (context, task) =>
            {
                var downstreamContext = new DownstreamContext(context);
                await firstDelegate.Invoke(downstreamContext);
            });

            return builder;
        }

        private static bool UsingEurekaServiceDiscoveryProvider(IInternalConfiguration configuration)
        {
            return configuration?.ServiceProviderConfiguration != null && configuration.ServiceProviderConfiguration.Type?.ToLower() == "eureka";
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

        private static async Task<IInternalConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            // make configuration from file system?
            // earlier user needed to add ocelot files in startup configuration stuff, asp.net will map it to this
            var fileConfig = (IOptions<FileConfiguration>)builder.ApplicationServices.GetService(typeof(IOptions<FileConfiguration>));
            
            // now create the config
            var internalConfigCreator = (IInternalConfigurationCreator)builder.ApplicationServices.GetService(typeof(IInternalConfigurationCreator));
            var internalConfig = await internalConfigCreator.Create(fileConfig.Value);

            // now save it in memory
            var internalConfigRepo = (IInternalConfigurationRepository)builder.ApplicationServices.GetService(typeof(IInternalConfigurationRepository));
            internalConfigRepo.AddOrReplace(internalConfig.Data);

            var fileConfigSetter = (IFileConfigurationSetter)builder.ApplicationServices.GetService(typeof(IFileConfigurationSetter));

            var fileConfigRepo = (IFileConfigurationRepository)builder.ApplicationServices.GetService(typeof(IFileConfigurationRepository));

            if (UsingConsul(fileConfigRepo))
            {
                await SetFileConfigInConsul(builder, fileConfigRepo, fileConfig, internalConfigCreator, internalConfigRepo);
            }
            else
            {
                await SetFileConfig(fileConfigSetter, fileConfig);
            }

            return GetOcelotConfigAndReturn(internalConfigRepo);
        }

        private static async Task SetFileConfigInConsul(IApplicationBuilder builder,
            IFileConfigurationRepository fileConfigRepo, IOptions<FileConfiguration> fileConfig,
            IInternalConfigurationCreator internalConfigCreator, IInternalConfigurationRepository internalConfigRepo)
        {
            // get the config from consul.
            var fileConfigFromConsul = await fileConfigRepo.Get();

            if (IsError(fileConfigFromConsul))
            {
                ThrowToStopOcelotStarting(fileConfigFromConsul);
            }
            else if (ConfigNotStoredInConsul(fileConfigFromConsul))
            {
                //there was no config in consul set the file in config in consul
                await fileConfigRepo.Set(fileConfig.Value);
            }
            else
            {
                // create the internal config from consul data
                var internalConfig = await internalConfigCreator.Create(fileConfigFromConsul.Data);

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
                else
                {
                    // add the internal config to the internal repo
                    var response = internalConfigRepo.AddOrReplace(internalConfig.Data);

                    if (IsError(response))
                    {
                        ThrowToStopOcelotStarting(response);
                    }
                }

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
            }

            //todo - this starts the poller if it has been registered...please this is so bad.
            var hack = builder.ApplicationServices.GetService(typeof(ConsulFileConfigurationPoller));
        }

        private static async Task SetFileConfig(IFileConfigurationSetter fileConfigSetter, IOptions<FileConfiguration> fileConfig)
        {
            Response response;
            response = await fileConfigSetter.Set(fileConfig.Value);

            if (IsError(response))
            {
                ThrowToStopOcelotStarting(response);
            }
        }

        private static bool ConfigNotStoredInConsul(Responses.Response<FileConfiguration> fileConfigFromConsul)
        {
            return fileConfigFromConsul.Data == null;
        }

        private static bool IsError(Response response)
        {
            return response == null || response.IsError;
        }

        private static IInternalConfiguration GetOcelotConfigAndReturn(IInternalConfigurationRepository provider)
        {
            var ocelotConfiguration = provider.Get();

            if(ocelotConfiguration?.Data == null || ocelotConfiguration.IsError)
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

        private static void CreateAdministrationArea(IApplicationBuilder builder, IInternalConfiguration configuration)
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

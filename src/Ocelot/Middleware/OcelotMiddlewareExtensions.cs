namespace Ocelot.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using DependencyInjection;
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
    using Rafty.Concensus.Node;
    using Microsoft.Extensions.DependencyInjection;

    public static class OcelotMiddlewareExtensions
    {
        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder)
        {
            await builder.UseOcelot(new OcelotPipelineConfiguration());
            return builder;
        }

        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, Action<OcelotPipelineConfiguration> pipelineConfiguration)
        {
            var config = new OcelotPipelineConfiguration();
            pipelineConfiguration?.Invoke(config);
            return await builder.UseOcelot(config);
        }

        public static async Task<IApplicationBuilder> UseOcelot(this IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            var configuration = await CreateConfiguration(builder);

            CreateAdministrationArea(builder, configuration);

            if (UsingRafty(builder))
            {
                SetUpRafty(builder);
            }

            if (UsingEurekaServiceDiscoveryProvider(configuration))
            {
                builder.UseDiscoveryClient();
            }

            ConfigureDiagnosticListener(builder);

            return CreateOcelotPipeline(builder, pipelineConfiguration);
            
        }

        private static IApplicationBuilder CreateOcelotPipeline(IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
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
            var node = builder.ApplicationServices.GetService<INode>();
            if (node != null)
            {
                return true;
            }

            return false;
        }

        private static void SetUpRafty(IApplicationBuilder builder)
        {
            var applicationLifetime = builder.ApplicationServices.GetService<IApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(() => OnShutdown(builder));
            var node = builder.ApplicationServices.GetService<INode>();
            var nodeId = builder.ApplicationServices.GetService<NodeId>();
            node.Start(nodeId);
        }

        private static async Task<IInternalConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            // make configuration from file system?
            // earlier user needed to add ocelot files in startup configuration stuff, asp.net will map it to this
            var fileConfig = builder.ApplicationServices.GetService<IOptions<FileConfiguration>>();

            // now create the config
            var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
            var internalConfig = await internalConfigCreator.Create(fileConfig.Value);
           //Configuration error, throw error message
            if (internalConfig.IsError)
            {
                ThrowToStopOcelotStarting(internalConfig);
            }

            // now save it in memory
            var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
            internalConfigRepo.AddOrReplace(internalConfig.Data);

            var fileConfigRepo = builder.ApplicationServices.GetService<IFileConfigurationRepository>();

            var adminPath = builder.ApplicationServices.GetService<IAdministrationPath>();

            if (UsingConsul(fileConfigRepo))
            {
                //Lots of jazz happens in here..check it out if you are using consul to store your config.
                await SetFileConfigInConsul(builder, fileConfigRepo, fileConfig, internalConfigCreator, internalConfigRepo);
            }
            else if(AdministrationApiInUse(adminPath))
            {
                //We have to make sure the file config is set for the ocelot.env.json and ocelot.json so that if we pull it from the 
                //admin api it works...boy this is getting a spit spags boll.
                var fileConfigSetter = builder.ApplicationServices.GetService<IFileConfigurationSetter>();

                await SetFileConfig(fileConfigSetter, fileConfig);
            }

            return GetOcelotConfigAndReturn(internalConfigRepo);
        }

        private static bool AdministrationApiInUse(IAdministrationPath adminPath)
        {
            return adminPath != null;
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
        }

        private static async Task SetFileConfig(IFileConfigurationSetter fileConfigSetter, IOptions<FileConfiguration> fileConfig)
        {
            var response = await fileConfigSetter.Set(fileConfig.Value);

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

            if (ocelotConfiguration?.Data == null || ocelotConfiguration.IsError)
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
            if (!string.IsNullOrEmpty(configuration.AdministrationPath))
            {
                builder.Map(configuration.AdministrationPath, app =>
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
        }

        private static void ConfigureDiagnosticListener(IApplicationBuilder builder)
        {
            var env = builder.ApplicationServices.GetService<IHostingEnvironment>();
            var listener = builder.ApplicationServices.GetService<OcelotDiagnosticListener>();
            var diagnosticListener = builder.ApplicationServices.GetService<DiagnosticListener>();
            diagnosticListener.SubscribeWithAdapter(listener);
        }

        private static void OnShutdown(IApplicationBuilder app)
        {
            var node = app.ApplicationServices.GetService<INode>();
            node.Stop();
        }
    }
}

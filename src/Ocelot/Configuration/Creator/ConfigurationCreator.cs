using System;
using System.Collections.Generic;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Configuration.Creator
{

    public class ConfigurationCreator : IConfigurationCreator
    {
        private readonly IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
        private readonly IQoSOptionsCreator _qosOptionsCreator;
        private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private readonly IAdministrationPath _adminPath;
        private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;

        public ConfigurationCreator(
            IServiceProviderConfigurationCreator serviceProviderConfigCreator,
            IQoSOptionsCreator qosOptionsCreator,
            IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
            IServiceProvider serviceProvider,
            ILoadBalancerOptionsCreator loadBalancerOptionsCreator
            )
        {
            _adminPath = serviceProvider.GetService<IAdministrationPath>();
            _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
            _serviceProviderConfigCreator = serviceProviderConfigCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        }

        public InternalConfiguration InternalConfiguration(FileConfiguration fileConfiguration, List<ReRoute> reRoutes)
        {
            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileConfiguration.GlobalConfiguration);

            var lbOptions = _loadBalancerOptionsCreator.CreateLoadBalancerOptions(fileConfiguration.GlobalConfiguration.LoadBalancerOptions);

            var qosOptions = _qosOptionsCreator.Create(fileConfiguration.GlobalConfiguration.QoSOptions);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileConfiguration.GlobalConfiguration.HttpHandlerOptions);

            var adminPath = _adminPath != null ? _adminPath.Path : null;

            return new InternalConfiguration(reRoutes,
                adminPath,
                serviceProviderConfiguration,
                fileConfiguration.GlobalConfiguration.RequestIdKey,
                lbOptions,
                fileConfiguration.GlobalConfiguration.DownstreamScheme,
                qosOptions,
                httpHandlerOptions
                );
        }
    }
}

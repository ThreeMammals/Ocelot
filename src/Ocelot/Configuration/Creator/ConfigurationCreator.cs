namespace Ocelot.Configuration.Creator
{
    using DependencyInjection;
    using File;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;

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

        public InternalConfiguration Create(FileConfiguration fileConfiguration, List<ReRoute> reRoutes)
        {
            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileConfiguration.GlobalConfiguration);

            var lbOptions = _loadBalancerOptionsCreator.Create(fileConfiguration.GlobalConfiguration.LoadBalancerOptions);

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

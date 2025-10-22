using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.Configuration.Creator;

public class ConfigurationCreator : IConfigurationCreator
{
    private readonly IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
    private readonly IQoSOptionsCreator _qosOptionsCreator;
    private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
    private readonly IAdministrationPath _adminPath;
    private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;
    private readonly IMetadataCreator _metadataCreator;
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;

    public ConfigurationCreator(
        IServiceProviderConfigurationCreator serviceProviderConfigCreator,
        IQoSOptionsCreator qosOptionsCreator,
        IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
        IServiceProvider serviceProvider,
        ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator,
        IMetadataCreator metadataCreator,
        IRateLimitOptionsCreator rateLimitOptionsCreator)
    {
        _adminPath = serviceProvider.GetService<IAdministrationPath>();
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _serviceProviderConfigCreator = serviceProviderConfigCreator;
        _qosOptionsCreator = qosOptionsCreator;
        _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
        _metadataCreator = metadataCreator;
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
    }

    public InternalConfiguration Create(FileConfiguration configuration, List<Route> routes)
    {
        var adminPath = _adminPath?.Path;
        var globalConfiguration = configuration.GlobalConfiguration ?? new();
        var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(globalConfiguration);
        var lbOptions = _loadBalancerOptionsCreator.Create(globalConfiguration.LoadBalancerOptions);
        var qosOptions = _qosOptionsCreator.Create(globalConfiguration.QoSOptions);
        var httpHandlerOptions = _httpHandlerOptionsCreator.Create(globalConfiguration.HttpHandlerOptions);
        var version = _versionCreator.Create(globalConfiguration.DownstreamHttpVersion);
        var versionPolicy = _versionPolicyCreator.Create(globalConfiguration.DownstreamHttpVersionPolicy);
        var metadataOptions = _metadataCreator.Create(null, globalConfiguration);
        var rateLimitOptions = _rateLimitOptionsCreator.Create(globalConfiguration);

        return new InternalConfiguration(routes,
            adminPath,
            serviceProviderConfiguration,
            globalConfiguration.RequestIdKey,
            lbOptions,
            globalConfiguration.DownstreamScheme,
            qosOptions,
            httpHandlerOptions,
            version,
            versionPolicy,
            metadataOptions,
            rateLimitOptions,
            globalConfiguration.Timeout);
    }
}

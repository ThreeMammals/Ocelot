using Microsoft.Extensions.DependencyInjection;
using Ocelot.Administration;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class ConfigurationCreator : IConfigurationCreator
{
    private readonly IAuthenticationOptionsCreator _authOptionsCreator;
    private readonly IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
    private readonly IQoSOptionsCreator _qosOptionsCreator;
    private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
    private readonly IAdministrationPath _adminPath;
    private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;
    private readonly IMetadataCreator _metadataCreator;
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
    private readonly ICacheOptionsCreator _cacheOptionsCreator;

    public ConfigurationCreator(
        IServiceProvider serviceProvider,
        IAuthenticationOptionsCreator authOptionsCreator,
        IServiceProviderConfigurationCreator serviceProviderConfigCreator,
        IQoSOptionsCreator qosOptionsCreator,
        IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
        ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator,
        IMetadataCreator metadataCreator,
        IRateLimitOptionsCreator rateLimitOptionsCreator,
        ICacheOptionsCreator cacheOptionsCreator)
    {
        _adminPath = serviceProvider.GetService<IAdministrationPath>();
        _authOptionsCreator = authOptionsCreator;
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _serviceProviderConfigCreator = serviceProviderConfigCreator;
        _qosOptionsCreator = qosOptionsCreator;
        _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
        _metadataCreator = metadataCreator;
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
        _cacheOptionsCreator = cacheOptionsCreator;
    }

    public InternalConfiguration Create(FileConfiguration configuration, Route[] routes)
    {
        var adminPath = _adminPath?.Path;
        var globalConfiguration = configuration.GlobalConfiguration ?? new();
        var authOptions = _authOptionsCreator.Create(globalConfiguration.AuthenticationOptions);
        var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(globalConfiguration);
        var lbOptions = _loadBalancerOptionsCreator.Create(globalConfiguration.LoadBalancerOptions);
        var qosOptions = _qosOptionsCreator.Create(globalConfiguration.QoSOptions);
        var httpHandlerOptions = _httpHandlerOptionsCreator.Create(globalConfiguration.HttpHandlerOptions);
        var version = _versionCreator.Create(globalConfiguration.DownstreamHttpVersion);
        var versionPolicy = _versionPolicyCreator.Create(globalConfiguration.DownstreamHttpVersionPolicy);
        var metadataOptions = _metadataCreator.Create(null, globalConfiguration);
        var rateLimitOptions = _rateLimitOptionsCreator.Create(globalConfiguration);
        var cacheOptions = _cacheOptionsCreator.Create(globalConfiguration.CacheOptions);

        return new InternalConfiguration(routes)
        {
            AdministrationPath = adminPath,
            AuthenticationOptions = authOptions,
            CacheOptions = cacheOptions,
            DownstreamHttpVersion = version,
            DownstreamHttpVersionPolicy = versionPolicy,
            DownstreamScheme = globalConfiguration.DownstreamScheme,
            HttpHandlerOptions = httpHandlerOptions,
            LoadBalancerOptions = lbOptions,
            MetadataOptions = metadataOptions,
            QoSOptions = qosOptions,
            RateLimitOptions = rateLimitOptions,
            RequestId = globalConfiguration.RequestIdKey,
            ServiceProviderConfiguration = serviceProviderConfiguration,
            Timeout = globalConfiguration.Timeout,
        };
    }
}

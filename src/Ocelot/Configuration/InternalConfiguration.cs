using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class InternalConfiguration : IInternalConfiguration
    {
        public InternalConfiguration(
            List<ReRoute> reRoutes, 
            string administrationPath, 
            ServiceProviderConfiguration serviceProviderConfiguration, 
            string requestId, 
            DynamicReRouteConfiguration dynamicReRouteConfiguration,
            LoadBalancerOptions loadBalancerOptions, 
            string downstreamScheme, 
            QoSOptions qoSOptions, 
            RateLimitGlobalOptions rateLimitOptions,
            HttpHandlerOptions httpHandlerOptions)
        {
            ReRoutes = reRoutes;
            AdministrationPath = administrationPath;
            ServiceProviderConfiguration = serviceProviderConfiguration;
            RequestId = requestId;
            LoadBalancerOptions = loadBalancerOptions;
            DownstreamScheme = downstreamScheme;
            DynamicReRouteConfiguration = dynamicReRouteConfiguration;
            QoSOptions = qoSOptions;
            RateLimitOptions = rateLimitOptions;
            HttpHandlerOptions = httpHandlerOptions;
        }

        public List<ReRoute> ReRoutes { get; }
        public string AdministrationPath {get;}
        public ServiceProviderConfiguration ServiceProviderConfiguration {get;}

        public DynamicReRouteConfiguration DynamicReRouteConfiguration { get; }

        public string RequestId {get;}
        public LoadBalancerOptions LoadBalancerOptions { get; }
        public string DownstreamScheme { get; }
        public QoSOptions QoSOptions { get; }
        public RateLimitGlobalOptions RateLimitOptions { get; }
        public HttpHandlerOptions HttpHandlerOptions { get; }
    }
}

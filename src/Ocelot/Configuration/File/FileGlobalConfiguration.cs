﻿using Ocelot.Configuration.Creator;

namespace Ocelot.Configuration.File
{
    public class FileGlobalConfiguration
    {
        public FileGlobalConfiguration()
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider();
            RateLimitOptions = new FileRateLimitOptions();
            LoadBalancerOptions = new FileLoadBalancerOptions();
            QoSOptions = new FileQoSOptions();
            HttpHandlerOptions = new FileHttpHandlerOptions();
            Metadata = new Dictionary<string, string>();
        }

        public string RequestIdKey { get; set; }

        public FileServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; }

        public FileRateLimitOptions RateLimitOptions { get; set; }

        public FileQoSOptions QoSOptions { get; set; }

        public string BaseUrl { get; set; }

        public FileLoadBalancerOptions LoadBalancerOptions { get; set; }

        public string DownstreamScheme { get; set; }

        public FileHttpHandlerOptions HttpHandlerOptions { get; set; }

        public string DownstreamHttpVersion { get; set; }

        /// <summary>The <see cref="HttpVersionPolicy"/> enum specifies behaviors for selecting and negotiating the HTTP version for a request.</summary>
        /// <value>A <see langword="string" /> value of defined <see cref="VersionPolicies"/> constants.</value>
        /// <remarks>
        /// Related to the <see cref="DownstreamHttpVersion"/> property.
        /// <list type="bullet">
        ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy">HttpVersionPolicy Enum</see></item>
        ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion">HttpVersion Class</see></item>
        ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy">HttpRequestMessage.VersionPolicy Property</see></item>
        /// </list>
        /// </remarks>
        public string DownstreamHttpVersionPolicy { get; set; }

        public IDictionary<string, string> Metadata { get; set; }
    }
}

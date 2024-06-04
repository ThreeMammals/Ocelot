using Ocelot.Configuration.Creator;

namespace Ocelot.Configuration.File
{
    public class FileDynamicRoute
    {
        public string ServiceName { get; set; }
        public FileRateLimitRule RateLimitRule { get; set; }
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

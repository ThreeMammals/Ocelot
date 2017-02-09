using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileReRoute
    {
        public FileReRoute()
        {
            AddHeadersToRequest = new Dictionary<string, string>();
            AddClaimsToRequest = new Dictionary<string, string>();
            RouteClaimsRequirement = new Dictionary<string, string>();
            AddQueriesToRequest = new Dictionary<string, string>();
            AuthenticationOptions = new FileAuthenticationOptions();
            FileCacheOptions = new FileCacheOptions();
        }

        public string DownstreamPathTemplate { get; set; }
        public string UpstreamTemplate { get; set; }
        public string UpstreamHttpMethod { get; set; }
        public FileAuthenticationOptions AuthenticationOptions { get; set; }
        public Dictionary<string, string> AddHeadersToRequest { get; set; }
        public Dictionary<string, string> AddClaimsToRequest { get; set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; set; }
        public Dictionary<string, string> AddQueriesToRequest { get; set; }
        public string RequestIdKey { get; set; }
        public FileCacheOptions FileCacheOptions { get; set; }
        public bool ReRouteIsCaseSensitive { get; set; }
        public string ServiceName { get; set; }
        public string DownstreamScheme {get;set;}
        public string DownstreamHost {get;set;}
        public int DownstreamPort { get; set; }
        public string LoadBalancer {get;set;}
    }
}
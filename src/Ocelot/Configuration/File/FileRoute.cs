namespace Ocelot.Configuration.File
{
    public class FileRoute : IRoute, ICloneable
    {
        public FileRoute()
        {
            AddClaimsToRequest = new Dictionary<string, string>();
            AddHeadersToRequest = new Dictionary<string, string>();
            AddQueriesToRequest = new Dictionary<string, string>();
            AuthenticationOptions = new FileAuthenticationOptions();
            ChangeDownstreamPathTemplate = new Dictionary<string, string>();
            DelegatingHandlers = new List<string>();
            DownstreamHeaderTransform = new Dictionary<string, string>();
            DownstreamHostAndPorts = new List<FileHostAndPort>();
            FileCacheOptions = new FileCacheOptions();
            HttpHandlerOptions = new FileHttpHandlerOptions();
            LoadBalancerOptions = new FileLoadBalancerOptions();
            Priority = 1;
            QoSOptions = new FileQoSOptions();
            RateLimitOptions = new FileRateLimitRule();
            RouteClaimsRequirement = new Dictionary<string, string>();
            SecurityOptions = new FileSecurityOptions();
            UpstreamHeaderTransform = new Dictionary<string, string>();
            UpstreamHttpMethod = new List<string>();
        }

        public FileRoute(FileRoute from)
        {
            DeepCopy(from, this);
        }

        public Dictionary<string, string> AddClaimsToRequest { get; set; }
        public Dictionary<string, string> AddHeadersToRequest { get; set; }
        public Dictionary<string, string> AddQueriesToRequest { get; set; }
        public FileAuthenticationOptions AuthenticationOptions { get; set; }
        public Dictionary<string, string> ChangeDownstreamPathTemplate { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
        public List<string> DelegatingHandlers { get; set; }
        public Dictionary<string, string> DownstreamHeaderTransform { get; set; }
        public List<FileHostAndPort> DownstreamHostAndPorts { get; set; }
        public string DownstreamHttpMethod { get; set; }
        public string DownstreamHttpVersion { get; set; }
        public string DownstreamPathTemplate { get; set; }
        public string DownstreamScheme { get; set; }
        public FileCacheOptions FileCacheOptions { get; set; }
        public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
        public string Key { get; set; }
        public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
        public int Priority { get; set; }
        public FileQoSOptions QoSOptions { get; set; }
        public FileRateLimitRule RateLimitOptions { get; set; }
        public string RequestIdKey { get; set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; set; }
        public bool RouteIsCaseSensitive { get; set; }
        public FileSecurityOptions SecurityOptions { get; set; }
        public string ServiceName { get; set; }
        public string ServiceNamespace { get; set; }
        public int Timeout { get; set; }
        public Dictionary<string, string> UpstreamHeaderTransform { get; set; }
        public string UpstreamHost { get; set; }
        public List<string> UpstreamHttpMethod { get; set; }
        public string UpstreamPathTemplate { get; set; }

        /// <summary>
        /// Clones this object by making a deep copy.
        /// </summary>
        /// <returns>A <see cref="FileRoute"/> deeply copied object.</returns>
        public object Clone()
        {
            var other = (FileRoute)MemberwiseClone();
            DeepCopy(this, other);
            return other;
        }

        public static void DeepCopy(FileRoute from, FileRoute to)
        {
            to.AddClaimsToRequest = new(from.AddClaimsToRequest);
            to.AddHeadersToRequest = new(from.AddHeadersToRequest);
            to.AddQueriesToRequest = new(from.AddQueriesToRequest);
            to.AuthenticationOptions = new(from.AuthenticationOptions);
            to.ChangeDownstreamPathTemplate = new(from.ChangeDownstreamPathTemplate);
            to.DangerousAcceptAnyServerCertificateValidator = from.DangerousAcceptAnyServerCertificateValidator;
            to.DelegatingHandlers = new(from.DelegatingHandlers);
            to.DownstreamHeaderTransform = new(from.DownstreamHeaderTransform);
            to.DownstreamHostAndPorts = from.DownstreamHostAndPorts.Select(x => new FileHostAndPort(x)).ToList();
            to.DownstreamHttpMethod = from.DownstreamHttpMethod;
            to.DownstreamHttpVersion = from.DownstreamHttpVersion;
            to.DownstreamPathTemplate = from.DownstreamPathTemplate;
            to.DownstreamScheme = from.DownstreamScheme;
            to.FileCacheOptions = new(from.FileCacheOptions);
            to.HttpHandlerOptions = new(from.HttpHandlerOptions);
            to.Key = from.Key;
            to.LoadBalancerOptions = new(from.LoadBalancerOptions);
            to.Priority = from.Priority;
            to.QoSOptions = new(from.QoSOptions);
            to.RateLimitOptions = new(from.RateLimitOptions);
            to.RequestIdKey = from.RequestIdKey;
            to.RouteClaimsRequirement = new(from.RouteClaimsRequirement);
            to.RouteIsCaseSensitive = from.RouteIsCaseSensitive;
            to.SecurityOptions = new(from.SecurityOptions);
            to.ServiceName = from.ServiceName;
            to.ServiceNamespace = from.ServiceNamespace;
            to.Timeout = from.Timeout;
            to.UpstreamHeaderTransform = new(from.UpstreamHeaderTransform);
            to.UpstreamHost = from.UpstreamHost;
            to.UpstreamHttpMethod = new(from.UpstreamHttpMethod);
            to.UpstreamPathTemplate = from.UpstreamPathTemplate;
        }
    }
}

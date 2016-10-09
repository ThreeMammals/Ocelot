namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class YamlReRoute
    {
        public string DownstreamTemplate { get; set; }
        public string UpstreamTemplate { get; set; }
        public string UpstreamHttpMethod { get; set; }
    }
}
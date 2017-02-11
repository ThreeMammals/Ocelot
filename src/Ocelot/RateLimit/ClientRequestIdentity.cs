namespace Ocelot.RateLimit
{
    public class ClientRequestIdentity
    {
        public string ClientId { get; set; }

        public string Path { get; set; }

        public string HttpVerb { get; set; }
    }
}
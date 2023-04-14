namespace Ocelot.RateLimit
{
    public class ClientRequestIdentity
    {
        public ClientRequestIdentity(string clientId, string path, string httpverb)
        {
            ClientId = clientId;
            Path = path;
            HttpVerb = httpverb;
        }

        public string ClientId { get; }

        public string Path { get; }

        public string HttpVerb { get; }
    }
}

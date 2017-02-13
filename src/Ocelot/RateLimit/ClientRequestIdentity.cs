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

        public string ClientId { get; private set; }

        public string Path { get; private set; }

        public string HttpVerb { get; private set; }
    }
}
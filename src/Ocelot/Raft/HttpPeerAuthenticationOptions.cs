namespace Ocelot.Raft
{
    public class HttpPeerAuthenticationOptions
    {
        public HttpPeerAuthenticationOptions(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username {get;private set;}
        public string Password {get;private set;}
    }
}
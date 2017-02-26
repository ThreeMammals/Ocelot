namespace Ocelot.Configuration.Provider
{
    public class User
    {
        public User(string subject, string userName, string hash, string salt)
        {
            Subject = subject;
            UserName = userName;
            Hash = hash;
            Salt = salt;
        }
        public string Subject { get; private set; }
        public string UserName { get; private set; }
        public string Hash { get; private set; }
        public string Salt { get; private set; }
    }
}
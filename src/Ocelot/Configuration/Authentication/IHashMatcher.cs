namespace Ocelot.Configuration.Authentication
{
    public interface IHashMatcher
    {
        bool Match(string password, string salt, string hash);
    }
}
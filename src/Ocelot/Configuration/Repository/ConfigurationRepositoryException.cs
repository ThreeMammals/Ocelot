namespace Ocelot.Configuration.Repository;

public class ConfigurationRepositoryException : Exception
{
    public ConfigurationRepositoryException(string message)
        : base(message) { }
}

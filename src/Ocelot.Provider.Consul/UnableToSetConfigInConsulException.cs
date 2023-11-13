namespace Ocelot.Provider.Consul;

public class UnableToSetConfigInConsulException : Exception
{
    public UnableToSetConfigInConsulException(string message)
        : base(message) { }
}

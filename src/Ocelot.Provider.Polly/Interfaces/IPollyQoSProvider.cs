namespace Ocelot.Provider.Polly.Interfaces
{
    public interface IPollyQoSProvider
    {
        CircuitBreaker CircuitBreaker { get; }
    }
}

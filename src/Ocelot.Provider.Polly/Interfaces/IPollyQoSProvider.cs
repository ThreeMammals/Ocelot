using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

public interface IPollyQoSProvider<TResult>
    where TResult : class
{
    CircuitBreaker<TResult> GetCircuitBreaker(DownstreamRoute route);
}

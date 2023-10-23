using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

public interface IPollyQoSProvider
{
    CircuitBreaker GetCircuitBreaker(DownstreamRoute route);
}

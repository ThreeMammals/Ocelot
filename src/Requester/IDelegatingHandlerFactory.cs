using Ocelot.Configuration;

namespace Ocelot.Requester;

public interface IDelegatingHandlerFactory
{
    List<DelegatingHandler> Get(DownstreamRoute route);
}

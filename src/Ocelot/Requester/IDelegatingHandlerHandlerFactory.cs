using Ocelot.Configuration;

namespace Ocelot.Requester;

public interface IDelegatingHandlerHandlerFactory
{
    List<DelegatingHandler> Get(DownstreamRoute route);
}

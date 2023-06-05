using Ocelot.Configuration;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteProviderFactory
    {
        IDownstreamRouteProvider Get(IInternalConfiguration config);
    }
}

using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IDownstreamAddressesCreator
    {
        List<DownstreamHostAndPort> Create(FileRoute route);
    }
}

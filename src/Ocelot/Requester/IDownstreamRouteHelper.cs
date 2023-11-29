using Ocelot.Configuration;

namespace Ocelot.Requester;

public interface IDownstreamRouteHelper
{
    public DownstreamRoute CurrentDownstreamRoute { get; set; }
}

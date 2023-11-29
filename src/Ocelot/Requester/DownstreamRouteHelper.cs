using Ocelot.Configuration;

namespace Ocelot.Requester;

public class DownstreamRouteHelper : IDownstreamRouteHelper
{
    private static readonly AsyncLocal<DownstreamRoute> Current = new();

    public DownstreamRoute CurrentDownstreamRoute
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}

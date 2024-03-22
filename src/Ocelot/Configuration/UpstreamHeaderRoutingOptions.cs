namespace Ocelot.Configuration;

public class UpstreamHeaderRoutingOptions
{
    public UpstreamHeaderRoutingOptions(IReadOnlyDictionary<string, ICollection<string>> headers, UpstreamHeaderRoutingTriggerMode mode)
    {
        Headers = new UpstreamRoutingHeaders(headers);
        Mode = mode;
    }

    public bool Enabled() => Headers.Any();

    public UpstreamRoutingHeaders Headers { get; }

    public UpstreamHeaderRoutingTriggerMode Mode { get; }
}

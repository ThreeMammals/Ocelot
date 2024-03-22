using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class UpstreamHeaderRoutingOptionsCreator : IUpstreamHeaderRoutingOptionsCreator
{
    public UpstreamHeaderRoutingOptions Create(FileUpstreamHeaderRoutingOptions options)
    {
        var mode = UpstreamHeaderRoutingTriggerMode.Any;
        if (options.TriggerOn.Length > 0)
        {
            mode = Enum.Parse<UpstreamHeaderRoutingTriggerMode>(options.TriggerOn, true);
        }

        // Keys are converted to uppercase as apparently that is the preferred
        // approach according to https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
        // Values are left untouched but value comparison at runtime is done in
        // a case-insensitive manner by using the appropriate StringComparer.
        var headers = options.Headers.ToDictionary(
            kv => kv.Key.ToUpperInvariant(),
            kv => kv.Value);

        return new UpstreamHeaderRoutingOptions(headers, mode);
    }
}

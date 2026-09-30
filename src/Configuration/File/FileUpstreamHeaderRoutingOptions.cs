namespace Ocelot.Configuration.File;

public class FileUpstreamHeaderRoutingOptions
{
    public IDictionary<string, ICollection<string>> Headers { get; set; } = new Dictionary<string, ICollection<string>>();

    public string TriggerOn { get; set; } = string.Empty;
}

namespace Ocelot.Configuration.File;

public class FileLoadBalancerOptions
{
    public FileLoadBalancerOptions()
    { }

    public FileLoadBalancerOptions(string type)
        : this()
    {
        Type = type;
    }

    public FileLoadBalancerOptions(FileLoadBalancerOptions from)
    {
        Expiry = from.Expiry;
        Key = from.Key;
        Type = from.Type;
    }

    public int? Expiry { get; set; }
    public string Key { get; set; }
    public string Type { get; set; }
}

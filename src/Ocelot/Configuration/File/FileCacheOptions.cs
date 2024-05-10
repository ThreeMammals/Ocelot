namespace Ocelot.Configuration.File;

public class FileCacheOptions
{
    public FileCacheOptions() { }

    public FileCacheOptions(FileCacheOptions from)
    {
        Region = from.Region;
        TtlSeconds = from.TtlSeconds;
        Header = from.Header;
        EnableContentHashing = from.EnableContentHashing;
    }

    /// <summary>
    /// using int? to have null as default value
    /// and allowing global configuration usage
    /// If null then use global configuration with 0 by default.
    /// </summary>
    /// <value>
    /// The time to live seconds, with 0 by default.
    /// </value>
    public int? TtlSeconds { get; set; }

    public string Region { get; set; }
    public string Header { get; set; }

    /// <summary>
    /// using bool? to have null as default value
    /// and allowing global configuration usage
    /// If null then use global configuration with false by default.
    /// </summary>
    /// <value>
    /// True if content hashing is enabled; otherwise, false.
    /// </value>
    public bool? EnableContentHashing { get; set; }
}

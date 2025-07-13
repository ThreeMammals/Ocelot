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

    /// <summary>Using <see cref="Nullable{T}"/> where T is <see cref="int"/> to have <see langword="null"/> as default value and allowing global configuration usage.</summary>
    /// <remarks>If <see langword="null"/> then use global configuration with 0 by default.</remarks>
    /// <value>The time to live seconds, with 0 by default.</value>
    public int? TtlSeconds { get; set; }
    public string Region { get; set; }
    public string Header { get; set; }

    /// <summary>Using <see cref="Nullable{T}"/> where T is <see cref="bool"/> to have <see langword="null"/> as default value and allowing global configuration usage.</summary>
    /// <remarks>If <see langword="null"/> then use global configuration with <see langword="false"/> by default.</remarks>
    /// <value><see langword="true"/> if content hashing is enabled; otherwise, <see langword="false"/>.</value>
    public bool? EnableContentHashing { get; set; }
}

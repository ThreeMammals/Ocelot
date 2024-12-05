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
        EnableFlexibleHashing = from.EnableFlexibleHashing;
        FlexibleHashingRegexes = from.FlexibleHashingRegexes;
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

    /// <summary>Using <see cref="Nullable{T}"/> where T is <see cref="bool"/> to have <see langword="null"/> as default value and allowing global configuration usage.</summary>
    /// <remarks>If <see langword="null"/> then use global configuration with <see langword="false"/> by default.</remarks>
    /// <value><see langword="true"/> if content flexible hashing is enabled; otherwise, <see langword="false"/>.</value>
    public bool? EnableFlexibleHashing { get; set; }

    /// <summary>Using <see cref="Nullable{T}"/> where T is <see cref="List{T}"/> to have <see langword="null"/> as default value and allowing global configuration usage.</summary>
    /// <remarks>If <see langword="null"/> then use global configuration with empty list by default.</remarks>
    /// <value>The list of regular expressions for flexible hashing.</value>
    public List<string>? FlexibleHashingRegexes { get; set; }
}

namespace Ocelot.Configuration.File;

public class FileRateLimitByIpRule : FileRateLimitRule
{
    /// <summary>A list of allowed client's IP addresses aka whitelisted ones.</summary>
    /// <value>An <see cref="IList{T}"/> collection of allowed IPs.</value>
    public IList<string> IPWhitelist { get; set; }
}

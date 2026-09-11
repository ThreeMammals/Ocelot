namespace Ocelot.Configuration.File;

public class FileRateLimitByMethodRule : FileRateLimitRule
{
    public HashSet<string> Methods { get; init; }
}

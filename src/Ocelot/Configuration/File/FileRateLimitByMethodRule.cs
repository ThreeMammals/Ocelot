namespace Ocelot.Configuration.File;

public class FileRateLimitByMethodRule : FileRateLimitRule
{
    public IList<string> Methods { get; init; }
}

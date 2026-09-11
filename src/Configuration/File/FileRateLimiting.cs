namespace Ocelot.Configuration.File;

public class FileRateLimiting : FileRateLimitRule
{
    public FileRateLimitByHeaderRule ByHeader { get; set; }
    public /*FileRateLimitByMethodRule*/ FileGlobalRateLimit ByMethod { get; set; } // a prototype solution must be designed. Methods -> GET, POST, PUT etc.
    public FileRateLimitByIpRule ByIP { get; set; } // a prototype solution must be designed. Based on RemoteIpAddress
    public FileRateLimitByAspNetRule ByAspNet { get; set; } // a prototype solution must be designed
    public IDictionary<string, string> Metadata { get; set; }
}

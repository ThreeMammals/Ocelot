namespace Ocelot.Configuration.File;

public class FileGlobalRateLimiting : FileRateLimitRule
{
    public FileGlobalRateLimitByHeaderRule[] ByHeader { get; set; }
    public /*FileGlobalRateLimitByMethodRule[]*/ FileGlobalRateLimit[] ByMethod { get; set; } // a prototype solution must be designed. Methods -> GET, POST, PUT etc.
    public FileGlobalRateLimitByIpRule[] ByIP { get; set; } // a prototype solution must be designed. Based on RemoteIpAddress
    public FileGlobalRateLimitByAspNetRule[] ByAspNet { get; set; } // a prototype solution must be designed
    public IDictionary<string, string> Metadata { get; set; }
}

namespace Ocelot.Configuration.File;

public sealed class FileGlobalRateLimit :
    FileGlobalRateLimitByHeaderRule, // TODO This is temporarily solution to inherit from RL by Header feature model, an extraction of props is required
    IRouteGroup
{
    // TODO Potentially, it should be 'Policy Name', or something that conveys the meaning of 'Rule Name'
    public string Name { get; init; }

    public string Pattern { get; init; }
}

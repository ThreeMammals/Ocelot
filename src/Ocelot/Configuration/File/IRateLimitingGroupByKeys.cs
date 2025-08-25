namespace Ocelot.Configuration.File;

internal interface IRateLimitingGroupByKeys
{
    IList<string> Keys { get; set; }
}

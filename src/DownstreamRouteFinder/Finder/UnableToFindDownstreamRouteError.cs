using Ocelot.Errors;
using Status = System.Net.HttpStatusCode;

namespace Ocelot.DownstreamRouteFinder.Finder;

public class UnableToFindDownstreamRouteError : Error
{
    public UnableToFindDownstreamRouteError(string path, string httpVerb)
        : base($"Failed to match route configuration for upstream: {httpVerb} {path}", OcelotErrorCode.UnableToFindDownstreamRouteError, (int)Status.NotFound)
    { }
}

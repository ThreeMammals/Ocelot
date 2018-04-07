using Ocelot.Errors;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class UnableToFindDownstreamRouteError : Error
    {
        public UnableToFindDownstreamRouteError(string path, string httpVerb) 
            : base($"Unable to find downstream route for path: {path}, verb: {httpVerb}", OcelotErrorCode.UnableToFindDownstreamRouteError)
        {
        }
    }
}

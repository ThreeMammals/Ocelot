using Ocelot.Errors;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class UnableToFindDownstreamRouteError : Error
    {
        public UnableToFindDownstreamRouteError() : base("UnableToFindDownstreamRouteError", OcelotErrorCode.UnableToFindDownstreamRouteError)
        {
        }
    }
}

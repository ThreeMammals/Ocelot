using Ocelot.Library.Errors;

namespace Ocelot.Library.DownstreamRouteFinder.Finder
{
    public class UnableToFindDownstreamRouteError : Error
    {
        public UnableToFindDownstreamRouteError() : base("UnableToFindDownstreamRouteError", OcelotErrorCode.UnableToFindDownstreamRouteError)
        {
        }
    }
}

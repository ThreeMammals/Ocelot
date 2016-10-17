namespace Ocelot.Library.DownstreamRouteFinder
{
    using Errors;

    public class UnableToFindDownstreamRouteError : Error
    {
        public UnableToFindDownstreamRouteError() : base("UnableToFindDownstreamRouteError", OcelotErrorCode.UnableToFindDownstreamRouteError)
        {
        }
    }
}

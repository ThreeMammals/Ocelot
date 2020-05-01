namespace Ocelot.DownstreamRouteFinder
{
    using Ocelot.Configuration;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using System.Collections.Generic;

    public class DownstreamRoute
    {
        public DownstreamRoute()
        {
        }

        public DownstreamRoute(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, ReRoute reRoute)
        {
            TemplatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            ReRoute = reRoute;
        }

        public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; private set; }
        public ReRoute ReRoute { get; private set; }
    }
}

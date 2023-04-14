namespace Ocelot.DownstreamRouteFinder
{
    using System.Collections.Generic;

    using Configuration;

    using UrlMatcher;

    public class DownstreamRouteHolder
    {
        public DownstreamRouteHolder()
        {
        }

        public DownstreamRouteHolder(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, Route route)
        {
            TemplatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            Route = route;
        }

        public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; }
        public Route Route { get; }
    }
}

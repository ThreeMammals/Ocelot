using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using System.Collections.Generic;

namespace Ocelot.DownstreamRouteFinder
{
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

using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.DownstreamRouteFinder
{
    public class DownstreamRoute
    {
        public DownstreamRoute(List<PlaceholderNameAndValue> templatePlaceholderNameAndValues, ReRoute reRoute)
        {
            TemplatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            ReRoute = reRoute;
        }
        public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; private set; }
        public ReRoute ReRoute { get; private set; }
        public object UpstreamHeadersFindAndReplace {get;private set;}
    }
}
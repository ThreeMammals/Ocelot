using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.DownstreamRouteFinder
{
    public class DownstreamRoute
    {
        public DownstreamRoute(List<UrlPathPlaceholderNameAndValue> templatePlaceholderNameAndValues, ReRoute reRoute)
        {
            TemplatePlaceholderNameAndValues = templatePlaceholderNameAndValues;
            ReRoute = reRoute;
        }
        public List<UrlPathPlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; private set; }
        public ReRoute ReRoute { get; private set; }
    }
}
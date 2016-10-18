using Ocelot.Library.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.Library.DownstreamRouteFinder
{
    using System.Collections.Generic;
    using Configuration;

    public class DownstreamRoute
    {
        public DownstreamRoute(List<TemplateVariableNameAndValue> templateVariableNameAndValues, ReRoute reRoute)
        {
            TemplateVariableNameAndValues = templateVariableNameAndValues;
            ReRoute = reRoute;
        }
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues { get; private set; }
        public ReRoute ReRoute { get; private set; }
    }
}
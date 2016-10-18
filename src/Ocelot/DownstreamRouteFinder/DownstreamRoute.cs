using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.DownstreamRouteFinder
{
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
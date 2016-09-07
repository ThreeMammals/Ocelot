using System.Collections.Generic;
using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.DownstreamRouteFinder
{
    public class DownstreamRoute
    {
        public DownstreamRoute(List<TemplateVariableNameAndValue> templateVariableNameAndValues, string downstreamUrlTemplate)
        {
            TemplateVariableNameAndValues = templateVariableNameAndValues;
            DownstreamUrlTemplate = downstreamUrlTemplate;
        }
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues { get; private set; }
        public string DownstreamUrlTemplate { get; private set; }
    }
}
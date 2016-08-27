using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlMatcher
{
    public class UrlMatch
    {
        public UrlMatch(bool match, List<TemplateVariableNameAndValue> templateVariableNameAndValues, string downstreamUrlTemplate)
        {
            Match = match; 
            TemplateVariableNameAndValues = templateVariableNameAndValues;
            DownstreamUrlTemplate = downstreamUrlTemplate;
        }
        public bool Match {get;private set;}
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues {get;private set;}
        public string DownstreamUrlTemplate {get;private set;}
    }
}
using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
    public class UrlPathMatch
    {
        public UrlPathMatch(bool match, List<TemplateVariableNameAndValue> templateVariableNameAndValues, string downstreamUrlPathTemplate)
        {
            Match = match; 
            TemplateVariableNameAndValues = templateVariableNameAndValues;
            DownstreamUrlPathTemplate = downstreamUrlPathTemplate;
        }
        public bool Match {get;private set;}
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues {get;private set;}
        public string DownstreamUrlPathTemplate {get;private set;}
    }
}
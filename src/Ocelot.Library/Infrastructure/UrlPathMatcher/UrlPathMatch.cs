using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
    public class UrlPathMatch
    {
        public UrlPathMatch(bool match, List<TemplateVariableNameAndValue> templateVariableNameAndValues, string urlPathTemplate)
        {
            Match = match; 
            TemplateVariableNameAndValues = templateVariableNameAndValues;
            UrlPathTemplate = urlPathTemplate;
        }
        public bool Match {get;private set;}
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues {get;private set;}

        public string UrlPathTemplate {get;private set;}
    }
}
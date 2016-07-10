using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
    public class UrlPathMatch
    {
        public UrlPathMatch(bool match, List<TemplateVariableNameAndValue> templateVariableNameAndValues)
        {
            Match = match; 
            TemplateVariableNameAndValues = templateVariableNameAndValues;
        }
        public bool Match {get;private set;}
        public List<TemplateVariableNameAndValue> TemplateVariableNameAndValues {get;private set;}
    }
}
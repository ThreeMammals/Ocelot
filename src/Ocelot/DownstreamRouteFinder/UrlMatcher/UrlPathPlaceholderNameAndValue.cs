namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class UrlPathPlaceholderNameAndValue
    {
        public UrlPathPlaceholderNameAndValue(string templateVariableName, string templateVariableValue)
        {
            TemplateVariableName = templateVariableName;
            TemplateVariableValue = templateVariableValue;
        }
        public string TemplateVariableName {get;private set;}
        public string TemplateVariableValue {get;private set;}
    }
}
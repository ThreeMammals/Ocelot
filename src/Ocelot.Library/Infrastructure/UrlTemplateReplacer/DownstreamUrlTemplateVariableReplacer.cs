using System.Text;
using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public class DownstreamUrlTemplateVariableReplacer : IDownstreamUrlTemplateVariableReplacer
    {
        public string ReplaceTemplateVariable(UrlMatch urlMatch)
        {
            var upstreamUrl = new StringBuilder();

            upstreamUrl.Append(urlMatch.DownstreamUrlTemplate);

            foreach (var templateVarAndValue in urlMatch.TemplateVariableNameAndValues)
            {
                upstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return upstreamUrl.ToString();
        }
    }
}
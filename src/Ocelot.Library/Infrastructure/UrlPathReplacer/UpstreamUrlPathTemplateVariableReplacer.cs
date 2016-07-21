using System;
using System.Text;
using Ocelot.Library.Infrastructure.UrlPathMatcher;

namespace Ocelot.Library.Infrastructure.UrlPathReplacer
{
    public class UpstreamUrlPathTemplateVariableReplacer : IUpstreamUrlPathTemplateVariableReplacer
    {
        public string ReplaceTemplateVariable(string upstreamPathTemplate, UrlPathMatch urlPathMatch)
        {
            var upstreamUrl = new StringBuilder();
            upstreamUrl.Append(upstreamPathTemplate);

            foreach (var templateVarAndValue in urlPathMatch.TemplateVariableNameAndValues)
            {
                upstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return upstreamUrl.ToString();
        }
    }
}
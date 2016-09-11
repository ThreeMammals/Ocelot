using System.Text;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public class DownstreamUrlTemplateVariableReplacer : IDownstreamUrlTemplateVariableReplacer
    {
        public string ReplaceTemplateVariables(DownstreamRoute downstreamRoute)
        {
            var upstreamUrl = new StringBuilder();

            upstreamUrl.Append(downstreamRoute.DownstreamUrlTemplate);

            foreach (var templateVarAndValue in downstreamRoute.TemplateVariableNameAndValues)
            {
                upstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return upstreamUrl.ToString();
        }
    }
}
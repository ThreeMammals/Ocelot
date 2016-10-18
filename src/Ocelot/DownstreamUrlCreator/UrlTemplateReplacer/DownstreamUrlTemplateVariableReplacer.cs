using System.Text;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class DownstreamUrlTemplateVariableReplacer : IDownstreamUrlTemplateVariableReplacer
    {
        public Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute)
        {
            var upstreamUrl = new StringBuilder();

            upstreamUrl.Append(downstreamRoute.ReRoute.DownstreamTemplate);

            foreach (var templateVarAndValue in downstreamRoute.TemplateVariableNameAndValues)
            {
                upstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return new OkResponse<string>(upstreamUrl.ToString());
        }
    }
}
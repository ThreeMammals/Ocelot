using System.Text;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.Responses;

namespace Ocelot.Library.DownstreamUrlCreator.UrlTemplateReplacer
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
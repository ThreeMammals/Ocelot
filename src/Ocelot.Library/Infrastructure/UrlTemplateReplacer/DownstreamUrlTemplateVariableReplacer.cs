using System.Text;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public class DownstreamUrlTemplateVariableReplacer : IDownstreamUrlTemplateVariableReplacer
    {
        public Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute)
        {
            var upstreamUrl = new StringBuilder();

            upstreamUrl.Append(downstreamRoute.DownstreamUrlTemplate);

            foreach (var templateVarAndValue in downstreamRoute.TemplateVariableNameAndValues)
            {
                upstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return new OkResponse<string>(upstreamUrl.ToString());
        }
    }
}
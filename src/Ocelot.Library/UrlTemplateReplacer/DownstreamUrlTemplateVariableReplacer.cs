namespace Ocelot.Library.UrlTemplateReplacer
{
    using System.Text;
    using DownstreamRouteFinder;
    using Responses;

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
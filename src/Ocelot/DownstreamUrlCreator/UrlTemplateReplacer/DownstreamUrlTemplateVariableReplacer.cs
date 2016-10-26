using System.Collections.Generic;
using System.Text;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class DownstreamUrlPathPlaceholderReplacer : IDownstreamUrlPathPlaceholderReplacer
    {
        public Response<DownstreamUrl> Replace(string downstreamTemplate, List<UrlPathPlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
        {
            var upstreamUrl = new StringBuilder();

            upstreamUrl.Append(downstreamTemplate);

            foreach (var placeholderVariableAndValue in urlPathPlaceholderNameAndValues)
            {
                upstreamUrl.Replace(placeholderVariableAndValue.TemplateVariableName, placeholderVariableAndValue.TemplateVariableValue);
            }

            return new OkResponse<DownstreamUrl>(new DownstreamUrl(upstreamUrl.ToString()));
        }
    }
}
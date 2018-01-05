using System.Collections.Generic;
using System.Text;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class DownstreamTemplatePathPlaceholderReplacer : IDownstreamPathPlaceholderReplacer
    {
        public Response<DownstreamPath> Replace(PathTemplate downstreamPathTemplate, List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
        {
            var downstreamPath = new StringBuilder();

            downstreamPath.Append(downstreamPathTemplate.Value);

            foreach (var placeholderVariableAndValue in urlPathPlaceholderNameAndValues)
            {
                downstreamPath.Replace(placeholderVariableAndValue.Name, placeholderVariableAndValue.Value);
            }

            return new OkResponse<DownstreamPath>(new DownstreamPath(downstreamPath.ToString()));
        }
    }
}
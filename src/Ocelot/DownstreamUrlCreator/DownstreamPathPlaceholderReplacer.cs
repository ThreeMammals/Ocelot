using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator
{
    public class DownstreamPathPlaceholderReplacer : IDownstreamPathPlaceholderReplacer
    {
        public Response<DownstreamPath> Replace(string downstreamPathTemplate,
            List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
        {
            var downstreamPath = new StringBuilder();

            downstreamPath.Append(downstreamPathTemplate);

            foreach (var placeholderVariableAndValue in urlPathPlaceholderNameAndValues)
            {
                downstreamPath.Replace(placeholderVariableAndValue.Name, placeholderVariableAndValue.Value);
            }

            return new OkResponse<DownstreamPath>(new DownstreamPath(downstreamPath.ToString()));
        }
    }
}

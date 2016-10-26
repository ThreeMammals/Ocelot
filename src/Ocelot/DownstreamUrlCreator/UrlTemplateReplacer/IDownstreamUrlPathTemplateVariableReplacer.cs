using System.Collections.Generic;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public interface IDownstreamUrlPathPlaceholderReplacer
    {
        Response<DownstreamUrl> Replace(string downstreamTemplate, List<UrlPathPlaceholderNameAndValue> urlPathPlaceholderNameAndValues);   
    }
}
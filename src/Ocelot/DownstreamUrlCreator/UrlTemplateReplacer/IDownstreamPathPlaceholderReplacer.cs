using System.Collections.Generic;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public interface IDownstreamPathPlaceholderReplacer
    {
        Response<DownstreamPath> Replace(PathTemplate downstreamPathTemplate, List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues);   
    }
}
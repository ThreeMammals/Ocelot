using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        string ReplaceTemplateVariable(DownstreamRoute downstreamRoute);   
    }
}
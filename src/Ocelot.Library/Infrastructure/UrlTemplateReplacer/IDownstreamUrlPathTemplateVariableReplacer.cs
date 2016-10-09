using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute);   
    }
}
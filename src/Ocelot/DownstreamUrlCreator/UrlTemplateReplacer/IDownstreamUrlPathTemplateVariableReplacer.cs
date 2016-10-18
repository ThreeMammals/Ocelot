using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute);   
    }
}
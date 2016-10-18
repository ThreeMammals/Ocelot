using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.Responses;

namespace Ocelot.Library.DownstreamUrlCreator.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        Response<string> ReplaceTemplateVariables(DownstreamRoute downstreamRoute);   
    }
}
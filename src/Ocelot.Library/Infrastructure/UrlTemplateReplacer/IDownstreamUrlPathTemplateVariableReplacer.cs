using Ocelot.Library.Infrastructure.UrlMatcher;

namespace Ocelot.Library.Infrastructure.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        string ReplaceTemplateVariable(UrlMatch urlMatch);   
    }
}
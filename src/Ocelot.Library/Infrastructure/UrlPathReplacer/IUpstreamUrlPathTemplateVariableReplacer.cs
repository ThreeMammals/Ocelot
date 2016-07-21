using Ocelot.Library.Infrastructure.UrlPathMatcher;

namespace Ocelot.Library.Infrastructure.UrlPathReplacer
{
    public interface IUpstreamUrlPathTemplateVariableReplacer
    {
        string ReplaceTemplateVariable(string upstreamPathTemplate, UrlPathMatch urlPathMatch);
        
    }
}
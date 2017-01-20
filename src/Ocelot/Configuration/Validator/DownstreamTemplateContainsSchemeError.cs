using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class DownstreamTemplateContainsSchemeError : Error
    {
        public DownstreamTemplateContainsSchemeError(string message) 
            : base(message, OcelotErrorCode.DownstreamTemplateContainsSchemeError)
        {
        }
    }
}

using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class DownstreamPathTemplateContainsSchemeError : Error
    {
        public DownstreamPathTemplateContainsSchemeError(string message) 
            : base(message, OcelotErrorCode.DownstreamPathTemplateContainsSchemeError)
        {
        }
    }
}

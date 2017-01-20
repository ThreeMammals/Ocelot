using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class DownstreamTemplateContainsHostError : Error
    {
        public DownstreamTemplateContainsHostError(string message) 
            : base(message, OcelotErrorCode.DownstreamTemplateContainsHostError)
        {
        }
    }
}

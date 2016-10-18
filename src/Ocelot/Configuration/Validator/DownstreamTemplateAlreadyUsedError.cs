using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class DownstreamTemplateAlreadyUsedError : Error
    {
        public DownstreamTemplateAlreadyUsedError(string message) : base(message, OcelotErrorCode.DownstreamTemplateAlreadyUsedError)
        {
        }
    }
}

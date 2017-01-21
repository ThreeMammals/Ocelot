using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class DownstreamPathTemplateAlreadyUsedError : Error
    {
        public DownstreamPathTemplateAlreadyUsedError(string message) : base(message, OcelotErrorCode.DownstreampathTemplateAlreadyUsedError)
        {
        }
    }
}

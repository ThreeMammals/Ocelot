using Ocelot.Library.Errors;

namespace Ocelot.Library.Configuration.Validator
{
    public class DownstreamTemplateAlreadyUsedError : Error
    {
        public DownstreamTemplateAlreadyUsedError(string message) : base(message, OcelotErrorCode.DownstreamTemplateAlreadyUsedError)
        {
        }
    }
}

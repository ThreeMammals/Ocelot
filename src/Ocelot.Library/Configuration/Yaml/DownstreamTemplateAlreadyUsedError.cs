namespace Ocelot.Library.Configuration.Yaml
{
    using Errors;

    public class DownstreamTemplateAlreadyUsedError : Error
    {
        public DownstreamTemplateAlreadyUsedError(string message) : base(message, OcelotErrorCode.DownstreamTemplateAlreadyUsedError)
        {
        }
    }
}

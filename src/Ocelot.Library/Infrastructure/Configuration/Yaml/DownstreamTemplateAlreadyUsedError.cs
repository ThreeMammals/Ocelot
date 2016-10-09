using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class DownstreamTemplateAlreadyUsedError : Error
    {
        public DownstreamTemplateAlreadyUsedError(string message) : base(message)
        {
        }
    }
}

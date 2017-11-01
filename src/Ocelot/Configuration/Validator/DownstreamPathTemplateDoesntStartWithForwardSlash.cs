using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class PathTemplateDoesntStartWithForwardSlash : Error
    {
        public PathTemplateDoesntStartWithForwardSlash(string message) 
            : base(message, OcelotErrorCode.PathTemplateDoesntStartWithForwardSlash)
        {
        }
    }
}

using Ocelot.Errors;

namespace Ocelot.Authorisation
{
    public class ScopeNotAuthorisedError : Error
    {
        public ScopeNotAuthorisedError(string message)
            : base(message, OcelotErrorCode.ScopeNotAuthorisedError)
        {
        }
    }
}

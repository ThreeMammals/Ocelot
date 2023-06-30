using Ocelot.Errors;

namespace Ocelot.Authorization
{
    public class ScopeNotAuthorizedError : Error
    {
        public ScopeNotAuthorizedError(string message)
            : base(message, OcelotErrorCode.ScopeNotAuthorizedError, 403)
        {
        }
    }
}

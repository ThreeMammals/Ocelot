using Ocelot.Errors;

namespace Ocelot.Claims.Parser
{
    public class CannotFindClaimError : Error
    {
        public CannotFindClaimError(string message) 
            : base(message, OcelotErrorCode.CannotFindClaimError)
        {
        }
    }
}

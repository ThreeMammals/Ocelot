using Ocelot.Errors;

namespace Ocelot.HeaderBuilder.Parser
{
    public class CannotFindClaimError : Error
    {
        public CannotFindClaimError(string message) 
            : base(message, OcelotErrorCode.CannotFindClaimError)
        {
        }
    }
}

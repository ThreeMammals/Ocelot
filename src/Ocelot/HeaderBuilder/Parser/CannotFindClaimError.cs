using Ocelot.Library.Errors;

namespace Ocelot.Library.HeaderBuilder.Parser
{
    public class CannotFindClaimError : Error
    {
        public CannotFindClaimError(string message) 
            : base(message, OcelotErrorCode.CannotFindClaimError)
        {
        }
    }
}

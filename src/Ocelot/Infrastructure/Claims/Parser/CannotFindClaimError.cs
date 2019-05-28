namespace Ocelot.Infrastructure.Claims.Parser
{
    using Errors;

    public class CannotFindClaimError : Error
    {
        public CannotFindClaimError(string message)
            : base(message, OcelotErrorCode.CannotFindClaimError)
        {
        }
    }
}

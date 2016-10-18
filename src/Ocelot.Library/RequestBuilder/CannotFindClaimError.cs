using Ocelot.Library.Errors;

namespace Ocelot.Library.RequestBuilder
{
    public class CannotFindClaimError : Error
    {
        public CannotFindClaimError(string message) 
            : base(message, OcelotErrorCode.CannotFindClaimError)
        {
        }
    }
}

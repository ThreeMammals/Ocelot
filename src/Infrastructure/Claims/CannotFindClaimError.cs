using Ocelot.Errors;

namespace Ocelot.Infrastructure.Claims;

public class CannotFindClaimError : Error
{
    public CannotFindClaimError(string message)
        : base(message, OcelotErrorCode.CannotFindClaimError, 403)
    {
    }
}

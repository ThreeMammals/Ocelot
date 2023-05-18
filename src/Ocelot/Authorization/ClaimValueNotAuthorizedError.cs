namespace Ocelot.Authorization
{
    using Errors;

    public class ClaimValueNotAuthorizedError : Error
    {
        public ClaimValueNotAuthorizedError(string message)
            : base(message, OcelotErrorCode.ClaimValueNotAuthorizedError, 403)
        {
        }
    }
}

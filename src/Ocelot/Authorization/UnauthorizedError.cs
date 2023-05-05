namespace Ocelot.Authorization
{
    using Errors;

    public class UnauthorizedError : Error
    {
        public UnauthorizedError(string message)
            : base(message, OcelotErrorCode.UnauthorizedError, 403)
        {
        }
    }
}

namespace Ocelot.Library.Middleware
{
    using Errors;

    public class UnauthenticatedError : Error
    {
        public UnauthenticatedError(string message) : base(message, OcelotErrorCode.UnauthenticatedError)
        {
        }
    }
}

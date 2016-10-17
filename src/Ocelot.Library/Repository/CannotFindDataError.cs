namespace Ocelot.Library.Repository
{
    using Errors;

    public class CannotFindDataError : Error
    {
        public CannotFindDataError(string message) : base(message, OcelotErrorCode.CannotFindDataError)
        {
        }
    }
}

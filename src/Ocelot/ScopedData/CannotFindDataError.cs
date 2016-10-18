using Ocelot.Errors;

namespace Ocelot.ScopedData
{
    public class CannotFindDataError : Error
    {
        public CannotFindDataError(string message) : base(message, OcelotErrorCode.CannotFindDataError)
        {
        }
    }
}

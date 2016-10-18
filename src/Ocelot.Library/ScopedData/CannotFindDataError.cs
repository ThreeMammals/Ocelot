using Ocelot.Library.Errors;

namespace Ocelot.Library.ScopedData
{
    public class CannotFindDataError : Error
    {
        public CannotFindDataError(string message) : base(message, OcelotErrorCode.CannotFindDataError)
        {
        }
    }
}

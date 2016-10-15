using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Repository
{
    public class CannotFindDataError : Error
    {
        public CannotFindDataError(string message) : base(message, OcelotErrorCode.CannotFindDataError)
        {
        }
    }
}

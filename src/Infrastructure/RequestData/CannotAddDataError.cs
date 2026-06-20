using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Infrastructure.RequestData;

public class CannotAddDataError : Error
{
    public CannotAddDataError(Exception exception)
        : base(exception, OcelotErrorCode.CannotAddDataError, StatusCodes.Status404NotFound)
    { }
}

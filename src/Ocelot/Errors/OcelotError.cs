using Microsoft.AspNetCore.Http;

namespace Ocelot.Errors;

public class OcelotError : Error
{
    public OcelotError()
        : base(string.Empty, OcelotErrorCode.UnknownError, StatusCodes.Status500InternalServerError)
    { }

    public OcelotError(string message)
        : base(message, OcelotErrorCode.UnknownError, StatusCodes.Status500InternalServerError)
    { }
}

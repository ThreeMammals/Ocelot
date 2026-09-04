using Microsoft.AspNetCore.Http;

namespace Ocelot.Errors;

public class UnknownError : Error
{
    public UnknownError()
        : base(string.Empty, OcelotErrorCode.UnknownError, StatusCodes.Status500InternalServerError)
    { }

    public UnknownError(string message)
        : base(message, OcelotErrorCode.UnknownError, StatusCodes.Status500InternalServerError)
    { }

    public UnknownError(Exception exception)
        : base(exception, OcelotErrorCode.UnknownError, StatusCodes.Status500InternalServerError)
    { }
}

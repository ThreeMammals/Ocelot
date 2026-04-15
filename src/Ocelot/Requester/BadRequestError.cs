using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Requester;

public class BadRequestError : Error
{
    public BadRequestError(string message)
        : base(message, OcelotErrorCode.BadRequestError, StatusCodes.Status400BadRequest)
    { }
}

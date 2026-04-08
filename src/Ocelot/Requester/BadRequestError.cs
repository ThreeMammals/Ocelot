using System;
using Ocelot.Errors;

namespace Ocelot.Requester;

public class BadRequestError : Error
{
    public BadRequestError(string message) 
        : base(message, OcelotErrorCode.BadRequestError, 400)
    {
    }
}
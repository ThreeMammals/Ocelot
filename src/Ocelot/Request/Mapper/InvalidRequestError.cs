using Ocelot.Errors;

namespace Ocelot.Request.Mapper;

public class InvalidRequestError : Error
{
    public InvalidRequestError(Exception exception) : base(exception.Message, OcelotErrorCode.InvalidRequestError, (int) System.Net.HttpStatusCode.BadRequest)
    {
    }
}

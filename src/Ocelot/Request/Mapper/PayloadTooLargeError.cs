using Ocelot.Errors;

namespace Ocelot.Request.Mapper;

public class PayloadTooLargeError : Error
{
    public PayloadTooLargeError(Exception exception) : base(exception.Message, OcelotErrorCode.PayloadTooLargeError, (int) System.Net.HttpStatusCode.RequestEntityTooLarge)
    {
    }
}

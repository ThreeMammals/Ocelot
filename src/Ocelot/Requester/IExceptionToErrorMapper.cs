using Ocelot.Errors;

namespace Ocelot.Requester
{
    public interface IExceptionToErrorMapper
    {
        Error Map(Exception exception);
    }
}

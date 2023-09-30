using Ocelot.Errors;

namespace Ocelot.Request.Mapper
{
    public class UnmappableRequestError : Error
    {
        public UnmappableRequestError(Exception exception) : base($"Error when parsing incoming request, exception: {exception}", OcelotErrorCode.UnmappableRequestError, 404)
        {
        }
    }
}

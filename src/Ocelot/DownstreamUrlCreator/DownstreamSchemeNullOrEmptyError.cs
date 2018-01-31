using Ocelot.Errors;

namespace Ocelot.DownstreamUrlCreator
{
    public class DownstreamSchemeNullOrEmptyError : Error
    {
        public DownstreamSchemeNullOrEmptyError()
            : base("downstream scheme was null or empty", OcelotErrorCode.DownstreamSchemeNullOrEmptyError)
        {
        }
    }
}
using Ocelot.Errors;

namespace Ocelot.DownstreamUrlCreator
{
    public class DownstreamHostNullOrEmptyError : Error
    {
        public DownstreamHostNullOrEmptyError()
            : base("downstream host was null or empty", OcelotErrorCode.DownstreamHostNullOrEmptyError)
        {
        }
    }
}
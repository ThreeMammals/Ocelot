using Ocelot.Errors;

namespace Ocelot.UnitTests.Responder
{
    internal class AnyError : Error
    {
        public AnyError() : base("blahh", OcelotErrorCode.UnknownError)
        {
        }

        public AnyError(OcelotErrorCode errorCode) : base("blah", errorCode)
        {
        }
    }
}

using Ocelot.Errors;

namespace Ocelot.UnitTests.Responder
{
    class AnyError : Error
    {
        public AnyError() : base("blahh", OcelotErrorCode.UnknownError)
        {
        }

        public AnyError(OcelotErrorCode errorCode) : base("blah", errorCode)
        {
        }
    }
}

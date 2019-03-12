using Ocelot.Errors;

namespace Ocelot.RateLimit
{
    public class QuotaExceededError : Error
    {
        public QuotaExceededError(string message)
            : base(message, OcelotErrorCode.QuotaExceededError)
        {
        }
    }
}

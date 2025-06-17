using Ocelot.Errors;

namespace Ocelot.RateLimiting;

public class QuotaExceededError : Error
{
    public QuotaExceededError(string message, int httpStatusCode)
        : base(message, OcelotErrorCode.QuotaExceededError, httpStatusCode)
    {
    }
}

using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.RateLimiting;

public interface IRateLimitCore
{
    string ComputeCounterKey(ClientRequestIdentity requestIdentity, RateLimitOptions option);
    TimeSpan ConvertToTimeSpan(string timeSpan);
    RateLimitHeaders GetRateLimitHeaders(HttpContext context, ClientRequestIdentity requestIdentity, RateLimitOptions option);
    RateLimitCounter ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitOptions option);
    int RetryAfterFrom(DateTime timestamp, RateLimitRule rule);
    void SaveRateLimitCounter(ClientRequestIdentity requestIdentity, RateLimitOptions option, RateLimitCounter counter, TimeSpan expirationTime);
}

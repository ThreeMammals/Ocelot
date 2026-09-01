using Microsoft.AspNetCore.Http;

namespace Ocelot.Errors;

/// <summary>
/// This error is intended to align with <see href="https://www.rfc-editor.org/info/rfc9110">RFC 9110</see> (HTTP Semantics),
/// specifically Section 15.6.5 "<see href="https://www.rfc-editor.org/info/rfc9110/#section-15.6.5">504 Gateway Timeout</see>".
/// </summary>
/// <param name="exception">Original exception object.</param>
public class RequestTimedOutError(Exception exception)
    : Error($"Timeout making http request, exception: {exception}",
        OcelotErrorCode.RequestTimedOutError, StatusCodes.Status504GatewayTimeout)
{ }

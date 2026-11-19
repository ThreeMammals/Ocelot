using Microsoft.AspNetCore.Http;

namespace Ocelot.ServiceDiscovery;

/// <summary>
/// Dynamic routing utilities that use multiple URL path segments as a service name.
/// </summary>
/// <param name="contextAccessor">The current <see cref="HttpContext"/> to access.</param>
public class MultipleSegmentUrlPathDynamicRouting(IHttpContextAccessor contextAccessor)
    : UrlPathDynamicRouting(contextAccessor)
{
    private const int MaxServiceNameEndingLength = 16;
    private string _sanitizedServiceNameEnding;

    protected virtual char ServiceNameDelimiter => '$';
    protected virtual string ServiceNameEnding => string.Empty;

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = DecodeUrlPath(upstreamUrlPath);
        if (path.Length <= 1)
            return SlashStr;

        int delimiter = FindServiceNameDelimiter(path, out int delimiterLength);
        if (delimiter < 0)
            return base.DownstreamPath(path.ToString());

        var downstreamPath = path[(delimiter + delimiterLength)..];
        return downstreamPath.Length == 0 || downstreamPath[0] == Slash
            ? downstreamPath.Length == 0 ? SlashStr : downstreamPath.ToString()
            : string.Concat(SlashStr.AsSpan(), downstreamPath); // ensure downstream path starts with a slash
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = DecodeUrlPath(upstreamUrlPath);
        if (path.Length <= 1)
            return base.GetServiceName(path.ToString());

        int delimiter = FindServiceNameDelimiter(path, out _);
        if (delimiter < 0)
            return base.GetServiceName(path.ToString());

        ReadOnlySpan<char> service = path[1..delimiter].TrimEnd(Slash);
        int dot = service.IndexOf(Dot);
        return dot != -1
            ? new(service[(dot + 1)..], service[..dot])
            : new(service);
    }

    private int FindServiceNameDelimiter(ReadOnlySpan<char> upstreamUrlPath, out int delimiterLength)
    {
        if (_sanitizedServiceNameEnding is null)
        {
            ReadOnlySpan<char> serviceNameEnding = ServiceNameEnding.AsSpan();
            Span<char> sanitizedEnding = stackalloc char[MaxServiceNameEndingLength];
            int sanitizedLength = SanitizeServiceNameEnding(serviceNameEnding, sanitizedEnding);
            _sanitizedServiceNameEnding = sanitizedEnding[..sanitizedLength].ToString();
        }

        ReadOnlySpan<char> delimiterString = _sanitizedServiceNameEnding.AsSpan();
        if (delimiterString.Length > 0)
        {
            delimiterLength = delimiterString.Length;
            int index = upstreamUrlPath[1..].IndexOf(delimiterString);
            return index < 0 ? -1 : index + 1;
        }

        delimiterLength = 1;
        char delimiter = ServiceNameDelimiter == Slash
            ? '$'
            : ServiceNameDelimiter;
        int charIndex = upstreamUrlPath[1..].IndexOf(delimiter);
        return charIndex < 0 ? -1 : charIndex + 1;
    }

    private static int SanitizeServiceNameEnding(ReadOnlySpan<char> serviceNameEnding, Span<char> sanitizedEnding)
    {
        if (serviceNameEnding.Length > MaxServiceNameEndingLength)
            throw new ArgumentOutOfRangeException(nameof(serviceNameEnding), $"The service name ending cannot exceed {MaxServiceNameEndingLength} characters.");

        int sanitizedLength = 0;
        foreach (char character in serviceNameEnding)
        {
            if (character != Slash)
                sanitizedEnding[sanitizedLength++] = character;
        }
        return sanitizedLength;
    }

    private static ReadOnlySpan<char> DecodeUrlPath(string upstreamUrlPath)
    {
        if (upstreamUrlPath is null)
            return [];

        ReadOnlySpan<char> path = upstreamUrlPath.AsSpan();
        return path.IndexOf('%') < 0
            ? path
            : Uri.UnescapeDataString(upstreamUrlPath).AsSpan();
    }
}

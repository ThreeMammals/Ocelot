using Microsoft.Extensions.Configuration;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Infrastructure;

/// <summary>
/// The configuration related implementation of the <see cref="IPlaceholders"/> interface.
/// </summary>
public partial class ConfigAwarePlaceholders : IPlaceholders
{
    private readonly IConfiguration _configuration;
    private readonly IPlaceholders _placeholders;

    public ConfigAwarePlaceholders(IConfiguration configuration, IPlaceholders placeholders)
    {
        _configuration = configuration;
        _placeholders = placeholders;
    }

    public Response<string> Get(string key)
    {
        var placeholderResponse = _placeholders.Get(key);

        if (!placeholderResponse.IsError)
        {
            return placeholderResponse;
        }

        return GetFromConfig(CleanKey(key));
    }

    public Response<string> Get(string key, DownstreamRequest request)
    {
        var placeholderResponse = _placeholders.Get(key, request);

        if (!placeholderResponse.IsError)
        {
            return placeholderResponse;
        }

        return GetFromConfig(CleanKey(key));
    }

    public Response Add(string key, Func<Response<string>> func)
        => _placeholders.Add(key, func);

    public Response Remove(string key)
        => _placeholders.Remove(key);

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[{}]", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex Regex();
#else
    private static readonly Regex _regex = RegexGlobal.New(@"[{}]");
    private static Regex Regex() => _regex;
#endif
    private static string CleanKey(string key)
        => Regex().Replace(key, string.Empty);

    private Response<string> GetFromConfig(string key)
    {
        var valueFromConfig = _configuration[key];
        return valueFromConfig == null
            ? new ErrorResponse<string>(new CouldNotFindPlaceholderError(key))
            : new OkResponse<string>(valueFromConfig);
    }
}

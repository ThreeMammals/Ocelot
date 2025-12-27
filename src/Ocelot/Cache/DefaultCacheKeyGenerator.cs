using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache;

public class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    public const char Delimiter = '-';
    
    public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
    {
        var builder = new StringBuilder()
            .Append(downstreamRequest.Method)
            .Append(Delimiter)
            .Append(downstreamRequest.OriginalString);

        var options = downstreamRoute.CacheOptions ?? new();
        if (!string.IsNullOrEmpty(options.Header))
        {
            var header = downstreamRequest.Headers.TryGetValues(options.Header, out var values)
                ? string.Join(string.Empty, values)
                : string.Empty;
            builder.Append(Delimiter).Append(header);
        }

        if (!options.EnableContentHashing || !downstreamRequest.HasContent)
        {
            return MD5Helper.GenerateMd5(builder.ToString());
        }

        var requestContent = await downstreamRequest.Request.Content.ReadAsStringAsync();
        builder.Append(Delimiter).Append(requestContent);
        return MD5Helper.GenerateMd5(builder.ToString());
    }
}

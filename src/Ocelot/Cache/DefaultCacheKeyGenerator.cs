using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache;

public class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    private const char Delimiter = '-';
    
    public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
    {
        var builder = new StringBuilder()
            .Append(downstreamRequest.Method)
            .Append(Delimiter)
            .Append(downstreamRequest.OriginalString);

        var options = downstreamRoute?.CacheOptions ?? new();
        if (!string.IsNullOrEmpty(options.Header))
        {
            var header = downstreamRequest.Headers
                .FirstOrDefault(r => r.Key.Equals(options.Header, StringComparison.OrdinalIgnoreCase))
                .Value?.FirstOrDefault();

            if (!string.IsNullOrEmpty(header))
            {
                builder.Append(Delimiter)
                    .Append(header);
            }
        }

        if (!options.EnableHeadersHashing && !options.EnableContentHashing && !downstreamRequest.HasContent)
        {
            return MD5Helper.GenerateMd5(builder.ToString());
        }

        if (options.EnableContentHashing)
        {
            var requestContentString = await ReadContentAsync(downstreamRequest);
            builder.Append(Delimiter)
                .Append(requestContentString);
        }
        
        if (options.EnableHeadersHashing)
        {
            var requestHeadersString = await ReadHeadersAsync(downstreamRequest);
            builder.Append(Delimiter)
                .Append(requestHeadersString);
        }

        if (options.CleanableHashingRegexes.Any())
        {
            return MD5Helper.GenerateMd5(HashingClean(builder.ToString(), options.CleanableHashingRegexes));
        }

        return MD5Helper.GenerateMd5(builder.ToString());
    }

    private static string HashingClean(string input, List<string> patterns) =>
        patterns.Aggregate(input, (current, pattern) => Regex.Replace(current, pattern, string.Empty, RegexOptions.Singleline));

    private static Task<string> ReadContentAsync(DownstreamRequest downstream) => downstream.HasContent
        ? downstream?.Request?.Content?.ReadAsStringAsync() ?? Task.FromResult(string.Empty)
        : Task.FromResult(string.Empty);

    private static Task<string> ReadHeadersAsync(DownstreamRequest downstream) => downstream.HasContent
        ? Task.FromResult(string.Join(":", downstream?.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value))))
        : Task.FromResult(string.Empty);
}

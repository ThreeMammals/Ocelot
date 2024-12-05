using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Ocelot.Configuration;
using Ocelot.Request.Middleware;
using System.Diagnostics;

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

        if (!options.EnableFlexibleHashing && !options.EnableContentHashing && !downstreamRequest.HasContent)
        {
            return MD5Helper.GenerateMd5(builder.ToString());
        }
        
        if (options.EnableContentHashing)
        {
            var requestContentString = await ReadContentAsync(downstreamRequest);
            builder.Append(Delimiter)
                .Append(requestContentString);
        }
        
        if (options.EnableFlexibleHashing)
        {
            var requestUriString = ReadUri(downstreamRequest);
            var requestHeadersString = ReadHeaders(downstreamRequest);
            builder.Append(Delimiter)
                .Append(requestUriString)
                .Append(Delimiter)
                .Append(requestHeadersString);
        }

        return MD5Helper.GenerateMd5(RegexClean(builder.ToString()));
    }

    private static string RegexClean(string input) => Regex.Replace(input, @"--[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}--|(--[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})", "--GUID--", RegexOptions.Singleline);

    private static string ReadUri(DownstreamRequest downstream) => downstream.HasContent
        ? downstream?.Request?.RequestUri?.ToString() ?? string.Empty
        : string.Empty;

    private static string ReadHeaders(DownstreamRequest downstream) => downstream.HasContent
        ? string.Join(":", downstream?.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value)))
        : string.Empty;

    private static Task<string> ReadContentAsync(DownstreamRequest downstream) => downstream.HasContent
        ? downstream?.Request?.Content?.ReadAsStringAsync() ?? Task.FromResult(string.Empty)
        : Task.FromResult(string.Empty);
}

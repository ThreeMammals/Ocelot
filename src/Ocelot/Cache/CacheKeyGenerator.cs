﻿using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        private const char Delimiter = '-';
        
        public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
        {
            var builder = new StringBuilder()
                .Append(downstreamRequest.Method)
                .Append(Delimiter)
                .Append(downstreamRequest.OriginalString);

            var cacheOptionsHeader = downstreamRoute?.CacheOptions?.Header;
            if (!string.IsNullOrEmpty(cacheOptionsHeader))
            {
                var header = downstreamRequest.Headers
                    .FirstOrDefault(r => r.Key.Equals(cacheOptionsHeader, StringComparison.OrdinalIgnoreCase))
                    .Value?.FirstOrDefault();

                if (!string.IsNullOrEmpty(header))
                {
                    builder.Append(Delimiter)
                        .Append(header);
                }
            }

            if (!downstreamRequest.HasContent)
            {
                return MD5Helper.GenerateMd5(builder.ToString());
            }

            var requestContentString = await ReadContentAsync(downstreamRequest);
            builder.Append(Delimiter)
                .Append(requestContentString);

            return MD5Helper.GenerateMd5(builder.ToString());
        }

        private static Task<string> ReadContentAsync(DownstreamRequest downstream) => downstream.HasContent
            ? downstream?.Request?.Content?.ReadAsStringAsync() ?? Task.FromResult(string.Empty)
            : Task.FromResult(string.Empty);
    }
}

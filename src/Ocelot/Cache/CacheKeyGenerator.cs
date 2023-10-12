using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamRequest downstreamRequest)
        {
            var builder = new StringBuilder($"{downstreamRequest.Method}-{downstreamRequest.OriginalString}");

            if (downstreamRequest.Headers?.TryGetValues("Content-Language", out IEnumerable<string> values) ?? false)
            {
                var contentLanguage = values.Any()
                    ? "-" + string.Join(string.Empty, values)
                    : string.Empty;
                builder.Append(contentLanguage);
            }

            if (downstreamRequest.Content != null)
            {
                var requestContentString = Task.Run(downstreamRequest.Content.ReadAsStringAsync).Result;
                builder.Append(requestContentString);
            }

            return MD5Helper.GenerateMd5(builder.ToString());
        }
    }
}

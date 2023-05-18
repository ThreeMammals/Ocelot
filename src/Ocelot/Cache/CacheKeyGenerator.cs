﻿namespace Ocelot.Cache
{
    using System.Text;
    using System.Threading.Tasks;

    using Ocelot.Request.Middleware;

    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamRequest downstreamRequest)
        {
            var downStreamUrlKeyBuilder = new StringBuilder($"{downstreamRequest.Method}-{downstreamRequest.OriginalString}");
            if (downstreamRequest.Content != null)
            {
                var requestContentString = Task.Run(async () => await downstreamRequest.Content.ReadAsStringAsync()).Result;
                downStreamUrlKeyBuilder.Append(requestContentString);
            }

            var hashedContent = MD5Helper.GenerateMd5(downStreamUrlKeyBuilder.ToString());
            return hashedContent;
        }
    }
}

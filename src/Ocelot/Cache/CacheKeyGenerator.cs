using Microsoft.Extensions.Primitives;
using Ocelot.Middleware;
using System.Text;
using System.Threading.Tasks;

using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamRequest downstreamRequest)
        {
            string hashedContent = null;
            string contentLanguage = "";
            if (context.HttpContext?.Request?.Headers?.TryGetValue("Content-Language", out StringValues values) ?? false)
            {
                contentLanguage = values.ToString();
            }
            StringBuilder downStreamUrlKeyBuilder = new StringBuilder($"{context.DownstreamRequest.Method}-{context.DownstreamRequest.OriginalString}{contentLanguage}");

            if (context.DownstreamRequest.Content != null)
            {
                var requestContentString = Task.Run(async () => await downstreamRequest.Content.ReadAsStringAsync()).Result;
                downStreamUrlKeyBuilder.Append(requestContentString);
            }

            var hashedContent = MD5Helper.GenerateMd5(downStreamUrlKeyBuilder.ToString());
            return hashedContent;
        }
    }
}

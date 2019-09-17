using Ocelot.Middleware;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamContext context)
        {
            string hashedContent = null;
            StringBuilder downStreamUrlKeyBuilder = new StringBuilder($"{context.DownstreamRequest.Method}-{context.DownstreamRequest.OriginalString}");
            if (context.DownstreamRequest.Content != null)
            {
                string requestContentString = Task.Run(async () => await context.DownstreamRequest.Content.ReadAsStringAsync()).Result;
                downStreamUrlKeyBuilder.Append(requestContentString);
            }

            hashedContent = MD5Helper.GenerateMd5(downStreamUrlKeyBuilder.ToString());
            return hashedContent;
        }
    }
}

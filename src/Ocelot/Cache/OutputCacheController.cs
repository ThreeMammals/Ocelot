using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;
using Ocelot.Configuration.Provider;

namespace Ocelot.Cache
{
    [Authorize]
    [Route("outputcache")]
    public class OutputCacheController : Controller
    {
        private IOcelotCache<CachedResponse> _cache;

        public OutputCacheController(IOcelotCache<CachedResponse> cache)
        {
            _cache = cache;
        }

        [HttpDelete]
        [Route("{region}")]
        public IActionResult Delete(string region)
        {
            _cache.ClearRegion(region);
            return new NoContentResult();
        }
    }
}
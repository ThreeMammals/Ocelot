using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;

namespace Ocelot.Controllers
{
    [Authorize]
    [Route("cache")]
    public class OutputCacheController : Controller
    {
        private IOcelotCache<HttpResponseMessage> _cache;

        public OutputCacheController(IOcelotCache<HttpResponseMessage> cache)
        {
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return new NotFoundResult();
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
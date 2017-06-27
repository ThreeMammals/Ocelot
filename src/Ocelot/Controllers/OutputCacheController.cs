using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;
using Ocelot.Configuration.Provider;

namespace Ocelot.Controllers
{
    [Authorize]
    [Route("cache")]
    public class OutputCacheController : Controller
    {
        private IOcelotCache<HttpResponseMessage> _cache;
        private IRegionsGetter _regionsGetter;

        public OutputCacheController(IOcelotCache<HttpResponseMessage> cache, IRegionsGetter regionsGetter)
        {
            _cache = cache;
            _regionsGetter = regionsGetter;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var regions = _regionsGetter.Regions();
            return new OkObjectResult(regions);
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
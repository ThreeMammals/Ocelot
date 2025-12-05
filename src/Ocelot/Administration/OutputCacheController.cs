using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;

namespace Ocelot.Administration;

// [ApiController] // TODO: Make it ApiController
//[Authorize(Policy = "OcelotAdministration")]
[Authorize]
[Route("outputcache")]
public class OutputCacheController : Controller
{
    private readonly IOcelotCache<CachedResponse> _cache;

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

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService.Controllers;

[Route("api/[controller]")]
public class CategoriesController : Controller
{
    // GET api/categories
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new[] { "category1", "category2" };
    }
}

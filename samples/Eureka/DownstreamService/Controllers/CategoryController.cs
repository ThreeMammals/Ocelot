using Microsoft.AspNetCore.Mvc;

namespace Ocelot.Samples.Eureka.DownstreamService.Controllers;

[Route("api/[controller]")]
public class CategoryController : Controller
{
    // GET api/category
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new[] { "category1", "category2" };
    }
}

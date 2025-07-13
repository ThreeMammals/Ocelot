namespace Ocelot.Samples.ServiceDiscovery.DownstreamService.Controllers;

[ApiController]
[Route("[controller]")]
public class CategoriesController : ControllerBase
{
    // GET /categories
    [HttpGet]
    public IEnumerable<string> Get()
    {
        var random = new Random();
        int max = DateTime.Now.Second;
        int length = random.Next(max);
        var categories = new List<string>(length);
        for (int i = 0; i < length; i++)
        {
            max = DateTime.Now.Millisecond < 3
                ? DateTime.Now.Millisecond + 3 : DateTime.Now.Millisecond;
            categories.Add("category" + random.Next(max));
        }

        return categories;
    }
}

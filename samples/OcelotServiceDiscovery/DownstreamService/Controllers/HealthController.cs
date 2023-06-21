using System.Reflection;

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService.Controllers;

using Models;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private static readonly DateTime startedAt = DateTime.Now;
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

    // GET /health
    [HttpGet]
    [Route("/health", Name = nameof(Health))]
    public MicroserviceResult Health()
    {
        // Analyze integrated services, get their health and return the Health flag
        bool isHealthy = true;

        // Get the link of the first action of current microservice workflow
        var link = Url.RouteUrl(routeName: "GetWeatherForecast", values: null, protocol: Request.Scheme);

        return new HealthResult
        {
            Healthy = isHealthy,
            Next = new Uri(link),
        };
    }

    // GET /ready
    [HttpGet]
    [Route("/ready", Name = nameof(Ready))]
    public MicroserviceResult Ready()
    {
        var asmName = assembly.GetName();

        //var link = Url.Action(action: nameof(Health), controller: nameof(Health), values: null, protocol: Request.Scheme);
        //var link = Url.RouteUrl(routeName: nameof(Health), values: null, protocol: Request.Scheme);
        var link = Url.Link(nameof(Health), null);

        return new ReadyResult
        {
            ServiceName = asmName.Name,
            ServiceVersion = asmName.Version.ToString(),
            StartedAt = startedAt,
            Next = new Uri(link),
        };
    }
}

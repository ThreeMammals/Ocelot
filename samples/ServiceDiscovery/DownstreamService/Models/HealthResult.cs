namespace Ocelot.Samples.ServiceDiscovery.DownstreamService.Models;

public class HealthResult : MicroserviceResult
{
    public bool Healthy { get; set; }
}

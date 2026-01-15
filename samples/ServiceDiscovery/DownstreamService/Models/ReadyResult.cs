using System;

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService.Models;

public class ReadyResult : MicroserviceResult
{
    public string ServiceName { get; set; }
    public string ServiceVersion { get; set; }
    public DateTime StartedAt { get; set; }
}

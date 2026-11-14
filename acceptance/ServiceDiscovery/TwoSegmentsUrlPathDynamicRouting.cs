using Microsoft.AspNetCore.Http;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Acceptance.ServiceDiscovery;

public class TwoSegmentsUrlPathDynamicRouting(IHttpContextAccessor contextAccessor)
    : UrlPathDynamicRouting(contextAccessor)
{
    public Func<string, string, string> ServiceNameMapping { get; set; }
        = (serviceName, version) => $"{serviceName}.service.{version}";

    public override string GetDownstreamPath()
        => DownstreamPath(Request.Path);

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        var path = upstreamUrlPath.AsSpan();
        var firstSegment = path[1..].IndexOf(Slash);
        if (firstSegment < 0)
        {
            return Slash.ToString();
        }

        var secondSegment = path[(firstSegment + 2)..].IndexOf(Slash);
        return secondSegment < 0
            ? Slash.ToString()
            : path[(firstSegment + secondSegment + 2)..].ToString();
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        var path = upstreamUrlPath.AsSpan();
        var service = base.GetServiceName(upstreamUrlPath);
        var firstSegment = path[1..].IndexOf(Slash);
        if (firstSegment < 0)
        {
            return new(CreateServiceName(service.Name, "web"), service.Namespace);
        }

        var secondSegment = path[(firstSegment + 2)..].IndexOf(Slash);
        var version = secondSegment < 0
            ? path[(firstSegment + 2)..].ToString()
            : path.Slice(firstSegment + 2, secondSegment).ToString();
        return new(CreateServiceName(service.Name, version), service.Namespace);
    }

    protected virtual string CreateServiceName(string serviceName, string version)
        => ServiceNameMapping(serviceName, version);
}

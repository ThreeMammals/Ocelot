using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IUpstreamHeaderRoutingOptionsCreator
{
    UpstreamHeaderRoutingOptions Create(FileUpstreamHeaderRoutingOptions options);
}

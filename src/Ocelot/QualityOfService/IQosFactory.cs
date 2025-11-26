using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.QualityOfService;

public interface IQoSFactory
{
    Response<DelegatingHandler> Get(DownstreamRoute request);
}

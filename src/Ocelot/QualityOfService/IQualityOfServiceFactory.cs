using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.QualityOfService;

public interface IQualityOfServiceFactory
{
    Response<DelegatingHandler> Get(DownstreamRoute request);
}

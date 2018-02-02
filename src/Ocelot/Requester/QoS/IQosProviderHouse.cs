using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester.QoS
{
    public interface IQosProviderHouse
    {
        Response<IQoSProvider> Get(ReRoute reRoute);
    }
}
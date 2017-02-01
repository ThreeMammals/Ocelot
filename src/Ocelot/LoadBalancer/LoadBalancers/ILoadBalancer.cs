using System;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Response<HostAndPort> Lease();
        Response Release(HostAndPort hostAndPort);
    }
}
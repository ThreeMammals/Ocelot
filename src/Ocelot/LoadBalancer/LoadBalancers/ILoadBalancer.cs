using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;
using System.Reflection;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    // TODO Add sync & async pairs
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext);

        void Release(ServiceHostAndPort hostAndPort);

        /// <summary>Static name of the load balancer instance.</summary>
        /// <remarks>To avoid reflection calls of the <see cref="MemberInfo.Name"/> property of the <see cref="System.Type"/> objects.</remarks>
        /// <value>A <see cref="string"/> object with type name value.</value>
        string Type { get; }
    }
}

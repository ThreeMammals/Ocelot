using Ocelot.Middleware;
using Ocelot.Responses;
using System.Threading.Tasks;

namespace Ocelot.Security
{
    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamContext context);
    }
}

using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IMultiplexer
    {
        Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next);
    }
}

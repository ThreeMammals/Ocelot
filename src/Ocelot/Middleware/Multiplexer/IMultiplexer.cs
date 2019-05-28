using Ocelot.Configuration;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IMultiplexer
    {
        Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next);
    }
}

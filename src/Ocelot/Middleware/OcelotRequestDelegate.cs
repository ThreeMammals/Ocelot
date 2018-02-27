using System.Threading.Tasks;

namespace Ocelot.Middleware
{
    public delegate Task OcelotRequestDelegate(DownstreamContext downstreamContext);
}

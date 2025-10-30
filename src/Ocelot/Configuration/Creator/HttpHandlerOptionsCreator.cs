using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.Configuration.Creator;

public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
{
    private readonly IOcelotTracer _tracer;
    public HttpHandlerOptionsCreator(IOcelotTracer tracer) => _tracer = tracer;

    public HttpHandlerOptions Create(FileHttpHandlerOptions options)
    {
        options ??= new();
        var useTracing = _tracer != null && options.UseTracing;
        return new(options)
        {
            UseTracing = useTracing,
        };
    }

    public HttpHandlerOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        throw new NotImplementedException();
    }

    public HttpHandlerOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        throw new NotImplementedException();
    }
}

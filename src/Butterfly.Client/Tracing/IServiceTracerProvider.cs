namespace Butterfly.Client.Tracing
{
    public interface IServiceTracerProvider
    {
        IServiceTracer GetServiceTracer();
    }
}
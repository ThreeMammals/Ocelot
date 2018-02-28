using AspectCore.DynamicProxy;

namespace Butterfly.Client.AspNetCore
{
    [NonAspect]
    public interface ITracingDiagnosticListener
    {
        string ListenerName { get; }
    }
}
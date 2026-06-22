namespace Ocelot.Logging;

public interface ITracingHandlerFactory
{
    ITracingHandler Get();
}

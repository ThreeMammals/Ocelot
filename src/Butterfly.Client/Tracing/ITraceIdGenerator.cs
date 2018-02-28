namespace Butterfly.Client.Tracing
{
    public interface ITraceIdGenerator
    {
        string Next();
    }
}
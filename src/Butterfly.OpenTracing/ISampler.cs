namespace Butterfly.OpenTracing
{
    public interface ISampler
    {
        bool ShouldSample();
    }
}
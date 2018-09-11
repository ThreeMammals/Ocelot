namespace Ocelot.LoadBalancer.Providers
{
    public interface IDownstreamRequestBaseHostProvider
    {
        BaseHostInfo GetBaseHostInfo(string downstreamHost);
    }
}

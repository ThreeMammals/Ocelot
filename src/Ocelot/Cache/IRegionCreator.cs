namespace Ocelot.Cache;

public interface IRegionCreator
{
    string Create(string region, string upstreamPathTemplate, IList<string> upstreamHttpMethod);
}

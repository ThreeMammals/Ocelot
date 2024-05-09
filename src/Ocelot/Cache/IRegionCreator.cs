using Ocelot.Configuration.File;

namespace Ocelot.Cache;

public interface IRegionCreator
{
    string Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethod);
}

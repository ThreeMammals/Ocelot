using Ocelot.Configuration;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamServiceFinder: IDownstreamServiceFinder
    {
        public const char Dot = '.';
        public const char Slash = '/';

        public string GetServiceName(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod, string upstreamHost, IInternalConfiguration configuration,
            out string serviceNamespace)
        {
            var path = upstreamUrlPath.AsSpan();
            int index = path[1..].IndexOf(Slash);
            var name = index == -1
                ? path[1..]
                : path.Slice(1, index).TrimEnd(Slash);

            index = name.IndexOf(Dot);
            serviceNamespace = index == -1
                ? string.Empty
                : name[..index].ToString();
            var serviceName = index == -1 ? name : name[++index..];
            return serviceName.ToString();
        }
    }
}

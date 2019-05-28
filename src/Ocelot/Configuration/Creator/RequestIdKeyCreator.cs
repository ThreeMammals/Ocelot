using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class RequestIdKeyCreator : IRequestIdKeyCreator
    {
        public string Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var reRouteId = !string.IsNullOrEmpty(fileReRoute.RequestIdKey);

            var requestIdKey = reRouteId
               ? fileReRoute.RequestIdKey
               : globalConfiguration.RequestIdKey;

            return requestIdKey;
        }
    }
}

using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class RequestIdKeyCreator : IRequestIdKeyCreator
    {
        public string Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var globalRequestIdConfiguration = !string.IsNullOrEmpty(globalConfiguration?.RequestIdKey);

             var requestIdKey = globalRequestIdConfiguration
                ? globalConfiguration.RequestIdKey
                : fileReRoute.RequestIdKey;

                return requestIdKey;
        }
    }
}
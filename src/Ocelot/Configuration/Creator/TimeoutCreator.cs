using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class TimeoutCreator : ITimeoutCreator
    {
        public int Create(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration)
        {
            return fileRoute.Timeout > 0 
                ? fileRoute.Timeout 
                : globalConfiguration.RequestTimeoutSeconds ?? 0;
        }
    }
}

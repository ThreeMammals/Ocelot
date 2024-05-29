using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface ITimeoutCreator
    {
        int Create(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration);
    }
}

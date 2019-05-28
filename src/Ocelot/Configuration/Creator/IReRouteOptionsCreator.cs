using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IReRouteOptionsCreator
    {
        ReRouteOptions Create(FileReRoute fileReRoute);
    }
}

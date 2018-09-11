using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IReRouteKeyCreator
    {
        string Create(FileReRoute fileReRoute);
    }
}

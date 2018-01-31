using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IQoSOptionsCreator
    {
        QoSOptions Create(FileReRoute fileReRoute);
    }
}
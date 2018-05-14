using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IQoSOptionsCreator
    {
        QoSOptions Create(FileQoSOptions options);
        QoSOptions Create(FileQoSOptions options, string pathTemplate, string[] httpMethods);
        QoSOptions Create(QoSOptions options, string pathTemplate, string[] httpMethods);
    }
}

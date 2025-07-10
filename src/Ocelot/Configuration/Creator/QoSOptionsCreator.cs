using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class QoSOptionsCreator : IQoSOptionsCreator
{
    public QoSOptions Create(FileQoSOptions options) => new(options);

    public QoSOptions Create(FileQoSOptions options, string pathTemplate, List<string> httpMethods)
        => Create(new QoSOptions(options), pathTemplate, httpMethods);

    public QoSOptions Create(QoSOptions options, string pathTemplate, List<string> httpMethods)
    {
        options.Key = $"{pathTemplate.FirstOrDefault()}|{string.Join(',', httpMethods)}";
        return options;
    }
}

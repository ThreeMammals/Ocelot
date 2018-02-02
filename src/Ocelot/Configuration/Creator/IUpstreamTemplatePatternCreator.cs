using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator
{
    public interface IUpstreamTemplatePatternCreator
    {
        UpstreamPathTemplate Create(FileReRoute reRoute);
    }
}
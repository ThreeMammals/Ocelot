using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IUpstreamTemplatePatternCreator
    {
        string Create(FileReRoute reRoute);
    }
}
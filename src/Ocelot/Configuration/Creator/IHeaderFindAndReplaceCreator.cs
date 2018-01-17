using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IHeaderFindAndReplaceCreator
    {
        HeaderTransformations Create(FileReRoute fileReRoute);
    }
}

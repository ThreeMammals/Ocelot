using Ocelot.Configuration.File;

namespace Ocelot.UnitTests;

public class FileUnitTest : FileUnit
{
    protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
}

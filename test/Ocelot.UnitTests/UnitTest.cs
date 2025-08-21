using Ocelot.Configuration.File;

namespace Ocelot.UnitTests;

public class UnitTest : Unit
{
    protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
}

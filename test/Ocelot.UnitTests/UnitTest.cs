using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests;

public class UnitTest : Unit
{
    //protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
    public override CancellationToken CancelMe => TestContext.Current.CancellationToken;
}

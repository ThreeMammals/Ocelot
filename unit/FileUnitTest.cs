using Ocelot.Configuration.File;

namespace Ocelot.UnitTests;

public class FileUnitTest : FileUnit
{
    protected static readonly string NL = Environment.NewLine;

    //protected static FileRouteBox<FileRoute> Box(FileRoute route) => new(route);
    public override CancellationToken CancelMe => TestContext.Current.CancellationToken;

    public static void EnsureThereIsNoFile(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
    }
}

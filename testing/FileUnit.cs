namespace Ocelot.Testing;

public class FileUnit : Unit, IDisposable
{
    protected string primaryConfigFileName;
    protected string globalConfigFileName;
    protected string environmentConfigFileName;
    protected readonly List<string> files;
    protected readonly List<string> folders;

    protected FileUnit() : this(null) { }

    protected FileUnit(string? folder)
    {
        folder ??= TestID;
        Directory.CreateDirectory(folder);
        folders = [folder];

        primaryConfigFileName = Path.Combine(folder, /*ConfigurationBuilderExtensions.PrimaryConfigFile*/ "ocelot.json");
        globalConfigFileName = Path.Combine(folder, /*ConfigurationBuilderExtensions.GlobalConfigFile*/ "ocelot.global.json");
        environmentConfigFileName = Path.Combine(folder, string.Format(/*ConfigurationBuilderExtensions.EnvironmentConfigFile*/"ocelot.{0}.json", EnvironmentName()));
        files =
        [
            primaryConfigFileName,
            globalConfigFileName,
            environmentConfigFileName,
        ];
    }

    protected virtual string EnvironmentName() => TestID;

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">Flag to trigger actual disposing operation.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DeleteFiles();
            DeleteFolders();
        }

        _disposed = true;
    }

    protected void DeleteFiles()
    {
        foreach (var file in files)
        {
            try
            {
                var f = new FileInfo(file);
                if (f.Exists)
                {
                    f.Delete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    protected void DeleteFolders()
    {
        foreach (var folder in folders)
        {
            try
            {
                var f = new DirectoryInfo(folder);
                if (f.Exists && f.FullName != AppContext.BaseDirectory)
                {
                    f.Delete(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}

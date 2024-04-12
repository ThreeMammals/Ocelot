using Ocelot.DependencyInjection;

namespace Ocelot.UnitTests;

public class FileUnitTest : UnitTest, IDisposable
{
    protected string _primaryConfigFileName;
    protected string _globalConfigFileName;
    protected string _environmentConfigFileName;
    protected readonly List<string> _files;
    protected readonly List<string> _folders;

    protected FileUnitTest() : this(null) { }

    protected FileUnitTest(string folder)
    {
        folder ??= TestID;
        Directory.CreateDirectory(folder);
        _folders = new() { folder };

        _primaryConfigFileName = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
        _globalConfigFileName = Path.Combine(folder, ConfigurationBuilderExtensions.GlobalConfigFile);
        _environmentConfigFileName = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, EnvironmentName()));
        _files = new()
        {
            _primaryConfigFileName,
            _globalConfigFileName,
            _environmentConfigFileName,
        };
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
        foreach (var file in _files)
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
        foreach (var folder in _folders)
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

    protected void TheOcelotPrimaryConfigFileExists(bool expected)
        => File.Exists(_primaryConfigFileName).ShouldBe(expected);
}

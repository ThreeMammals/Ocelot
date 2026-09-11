using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using FileIO = System.IO.File;

namespace Ocelot.Configuration.Repository;
public class DiskFileConfigurationRepository : IFileConfigurationRepository, IDisposable
{
    private FileInfo _ocelotFile;
    private FileInfo _environmentFile;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // 1 concurrent access

    public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment, IOcelotConfigurationChangeTokenSource changeTokenSource)
    {
        _hostingEnvironment = hostingEnvironment;
        _changeTokenSource = changeTokenSource;
        Initialize(AppContext.BaseDirectory);
    }

    public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment, IOcelotConfigurationChangeTokenSource changeTokenSource, string folder)
    {
        _hostingEnvironment = hostingEnvironment;
        _changeTokenSource = changeTokenSource;
        Initialize(folder);
    }

    private void Initialize(string folder)
    {
        folder ??= AppContext.BaseDirectory;
        _ocelotFile = new FileInfo(Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile));

        var envFile = !string.IsNullOrEmpty(_hostingEnvironment.EnvironmentName)
            ? string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, _hostingEnvironment.EnvironmentName)
            : ConfigurationBuilderExtensions.PrimaryConfigFile;

        _environmentFile = new FileInfo(Path.Combine(folder, envFile));
    }

    public FileConfiguration Get()
    {
        string json;
        _semaphore.Wait();
        try
        {
            json = FileIO.ReadAllText(_environmentFile.FullName);
        }
        finally
        {
            _semaphore.Release();
        }

        return JsonConvert.DeserializeObject<FileConfiguration>(json);
    }

    public async Task<FileConfiguration> GetAsync(CancellationToken cancellationToken = default)
    {
        string json;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            json = await FileIO.ReadAllTextAsync(_environmentFile.FullName, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }

        return JsonConvert.DeserializeObject<FileConfiguration>(json);
    }

    public void Set(FileConfiguration configuration)
    {
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        _semaphore.Wait();
        try
        {
            if (_environmentFile.Exists)
                _environmentFile.Delete();

            FileIO.WriteAllText(_environmentFile.FullName, json);

            if (_ocelotFile.Exists)
                _ocelotFile.Delete();

            FileIO.WriteAllText(_ocelotFile.FullName, json);
        }
        finally
        {
            _semaphore.Release();
        }

        _changeTokenSource.Activate();
    }

    public async Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_environmentFile.Exists)
                _environmentFile.Delete();

            await FileIO.WriteAllTextAsync(_environmentFile.FullName, json, cancellationToken);

            if (_ocelotFile.Exists)
                _ocelotFile.Delete();

            await FileIO.WriteAllTextAsync(_ocelotFile.FullName, json, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }

        _changeTokenSource.Activate();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}

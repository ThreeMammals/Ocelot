using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.Configuration.Repository;

public class FileConfigurationPoller : IHostedService, IDisposable
{
    private readonly IOcelotLogger _logger;
    private readonly IFileConfigurationRepository _repo;
    private string _previousAsJson;
    private Timer _timer;
    private volatile bool _polling;
    private readonly IFileConfigurationPollerOptions _options;
    private readonly IInternalConfigurationRepository _internalConfigRepo;
    private readonly IInternalConfigurationCreator _internalConfigCreator;

    public FileConfigurationPoller(
        IOcelotLoggerFactory factory,
        IFileConfigurationRepository repo,
        IFileConfigurationPollerOptions options,
        IInternalConfigurationRepository internalConfigRepo,
        IInternalConfigurationCreator internalConfigCreator)
    {
        _internalConfigRepo = internalConfigRepo;
        _internalConfigCreator = internalConfigCreator;
        _options = options;
        _logger = factory.CreateLogger<FileConfigurationPoller>();
        _repo = repo;
        _previousAsJson = string.Empty;
    }

    private void OnTimer(object state)
    {
        if (_polling)
            return;

        _polling = true;
        PollAsync().GetAwaiter().GetResult(); // TODO This is not good, TimerCallback must be synchronous
        _polling = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_timer is not null)
            return Task.CompletedTask;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is starting.");
        _timer = new(OnTimer, null, _options.Delay, _options.Delay); // TODO state could be CancellationToken?
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer is null)
            return Task.CompletedTask;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is stopping.");
        _timer.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task PollAsync()
    {
        _logger.LogInformation(() => $"{nameof(PollAsync)}: Started polling");

        var fileConfig = await _repo.Get();
        if (fileConfig.IsError)
        {
            _logger.LogWarning(() => $"{nameof(PollAsync)}: Error getting file config, errors are {string.Join(',', fileConfig.Errors.Select(x => x.Message))}");
            return;
        }

        var asJson = ToJson(fileConfig.Data);
        if (asJson != _previousAsJson)
        {
            var config = await _internalConfigCreator.Create(fileConfig.Data);
            if (!config.IsError)
                _internalConfigRepo.AddOrReplace(config.Data);

            _previousAsJson = asJson;
        }

        _logger.LogInformation(() => $"{nameof(PollAsync)}: Finished polling");
    }

    /// <summary>
    /// We could do object comparison here but performance isnt really a problem. This might be an issue one day!.
    /// </summary>
    /// <returns>hash of the config.</returns>
    private static string ToJson(FileConfiguration config)
    {
        var currentHash = JsonConvert.SerializeObject(config);
        return currentHash;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        GC.SuppressFinalize(this);
    }
}

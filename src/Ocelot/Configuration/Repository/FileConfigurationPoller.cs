using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.Configuration.Repository;

public class FileConfigurationPoller : IFileConfigurationPoller, IHostedService, IDisposable
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
        Poll(); // PollAsync().GetAwaiter().GetResult(); // TODO This is not good, TimerCallback must be synchronous
        _polling = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_timer is not null)
            return;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is starting.");
        int delay = await _options.DelayAsync(cancellationToken);
        _timer = new(OnTimer, null, delay, delay); // TODO state could be CancellationToken?
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_timer is null)
            return Task.CompletedTask;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is stopping.");
        return Task.Run(() => _timer.Change(Timeout.Infinite, 0), cancellationToken);
    }

    public void Poll()
    {
        if (_polling)
            return;

        _logger.LogInformation(() => $"{nameof(Poll)}: Started polling");
        FileConfiguration configuration;
        try
        {
            configuration = _repo.Get();
        }
        catch (Exception e)
        {
            _logger.LogWarning(() => $"{nameof(Poll)}: Error getting {nameof(FileConfiguration)} -> {e}.");
            return;
        }

        if (configuration is null)
        {
            _logger.LogWarning(() => $"{nameof(Poll)}: Null object while getting {nameof(FileConfiguration)} via the {_repo.GetType().Name} service.");
            return;
        }

        var asJson = ToJson(configuration);
        if (asJson != _previousAsJson)
        {
            var config = _internalConfigCreator.Create(configuration).GetAwaiter().GetResult(); // TODO Extend interface with sync version
            if (!config.IsError)
                _internalConfigRepo.AddOrReplace(config.Data);

            _previousAsJson = asJson;
        }

        _logger.LogInformation(() => $"{nameof(Poll)}: Finished polling");
    }

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        if (_polling)
            return;

        _logger.LogInformation(() => $"{nameof(PollAsync)}: Started polling");
        FileConfiguration configuration;
        try
        {
            configuration = await _repo.GetAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(() => $"{nameof(PollAsync)}: Error getting {nameof(FileConfiguration)} -> {e}.");
            return;
        }

        if (configuration is null)
        {
            _logger.LogWarning(() => $"{nameof(PollAsync)}: Null object while getting {nameof(FileConfiguration)} via the {_repo.GetType().Name} service.");
            return;
        }

        var asJson = ToJson(configuration);
        if (asJson != _previousAsJson)
        {
            var config = await _internalConfigCreator.Create(configuration);
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

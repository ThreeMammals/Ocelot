using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.Configuration.Repository;

/// <summary>
/// This hosted service pools periodically (~1 sec) configuration data from the <see cref="IFileConfigurationRepository"/> and propagates data into the <see cref="IInternalConfigurationRepository"/> to be reused across Ocelot services.
/// Thus, this service is responsible for getting actual configuration from anywhere and reflect the state to internal Ocelot repo.
/// </summary>
/// <remarks>
/// Feature: <see cref="IOcelotBuilder.AddConfigurationPoller()"/>.<br/>
/// Feature PR: <see href="https://github.com/ThreeMammals/Ocelot/pull/157/">157</see>.<br/>
/// Note, that the service is reused in Consul service discovery provider.
/// </remarks>
public class FileConfigurationPoller : IFileConfigurationPoller, IHostedService, IDisposable
{
    private readonly IOcelotLogger _logger;
    private readonly IFileConfigurationRepository _repo;
    private string _previousAsJson;
    private Timer _timer;
    private int _isPolling;
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
        Poll(); // PollAsync().GetAwaiter().GetResult(); // TODO This is not good, TimerCallback must be synchronous
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_timer is not null)
            return;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is starting.");
        int delay = await _options.DelayAsync(cancellationToken);
        _timer = new(OnTimer, null, delay, delay); // TODO state could be CancellationToken?
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var timer = Interlocked.Exchange(ref _timer, null);
        if (timer is null)
            return;

        _logger.LogInformation(() => $"{nameof(FileConfigurationPoller)} is stopping.");
        timer.Change(Timeout.Infinite, Timeout.Infinite);
        await DisposeTimerAsync(timer, cancellationToken);
    }

    public void Poll()
    {
        if (!TryEnterPolling())
            return;
        try
        {
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
        finally
        {
            ExitPolling();
        }
    }

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        if (!TryEnterPolling())
            return;
        try
        {
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
        finally
        {
            ExitPolling();
        }
    }

    /// <summary>
    /// We could do object comparison here but performance isnt really a problem. This might be an issue one day!.
    /// </summary>
    /// <returns>hash of the config.</returns>
    private static string ToJson(FileConfiguration config)
    {
        var currentHash = JsonConvert.SerializeObject(config); // TODO WTF?
        return currentHash;
    }

    public void Dispose()
    {
        var timer = Interlocked.Exchange(ref _timer, null);
        if (timer is not null)
            DisposeTimer(timer);
        GC.SuppressFinalize(this);
    }

    private bool TryEnterPolling() => Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0;

    private void ExitPolling() => Volatile.Write(ref _isPolling, 0);

    private static async Task DisposeTimerAsync(Timer timer, CancellationToken cancellationToken)
    {
        using var disposed = new ManualResetEvent(false);
        if (!timer.Dispose(disposed))
            return;

        while (!disposed.WaitOne(0))
            await Task.Delay(10, cancellationToken);
    }

    private static void DisposeTimer(Timer timer)
    {
        using var disposed = new ManualResetEvent(false);
        if (!timer.Dispose(disposed))
            return;

        disposed.WaitOne();
    }
}

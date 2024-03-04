using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Repository
{
    public sealed class FileConfigurationPoller : IHostedService, IDisposable
    {
        private readonly IOcelotLogger _logger;
        private readonly IFileConfigurationRepository _repo;
        private string _previousAsJson;
        private Timer _timer;
        private bool _polling;
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(FileConfigurationPoller)} is starting.");

            _timer = new Timer(async x =>
            {
                if (_polling)
                {
                    return;
                }

                _polling = true;
                await Poll();
                _polling = false;
            }, null, _options.Delay, _options.Delay);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(FileConfigurationPoller)} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async Task Poll()
        {
            _logger.LogInformation($"Started {nameof(Poll)}");
            try
            {
                var fileConfig = await _repo.GetAsync();
                var asJson = ToJson(fileConfig);
                if (asJson != _previousAsJson)
                {
                    var config = await _internalConfigCreator.Create(fileConfig);
                    if (!config.IsError)
                    {
                        _internalConfigRepo.AddOrReplace(config.Data);
                    }

                    _previousAsJson = asJson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(() => $"Error getting file config! Errors are:{Environment.NewLine}{ex.AllMessages}");
                return;
            }
            finally
            {
                _logger.LogInformation($"Finished {nameof(Poll)}");
            }
        }

        /// <summary>
        /// We could do object comparison here but performance isnt really a problem. This might be an issue one day.
        /// </summary>
        /// <returns>A <see langword="string"/> with current hash of the config.</returns>
        private static string ToJson(FileConfiguration config) => JsonConvert.SerializeObject(config);

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Logging;

namespace Ocelot.Configuration.Repository
{
    public class ConsulFileConfigurationPoller : IDisposable
    {
        private readonly IOcelotLogger _logger; 
        private readonly IFileConfigurationRepository _repo;
        private readonly IFileConfigurationSetter _setter;
        private string _previousAsJson;
        private readonly Timer _timer;
        private bool _polling;
        private readonly IConsulPollerConfiguration _config;

        public ConsulFileConfigurationPoller(
            IOcelotLoggerFactory factory, 
            IFileConfigurationRepository repo, 
            IFileConfigurationSetter setter, 
            IConsulPollerConfiguration config)
        {
            _setter = setter;
            _config = config;
            _logger = factory.CreateLogger<ConsulFileConfigurationPoller>();
            _repo = repo;
            _previousAsJson = "";
            _timer = new Timer(async x =>
            {
                if(_polling)
                {
                    return;
                }
                
                _polling = true;
                await Poll();
                _polling = false;
            }, null, _config.Delay, _config.Delay);
        }
        
        private async Task Poll()
        {
            _logger.LogInformation("Started polling consul");

            var fileConfig = await _repo.Get();

            if(fileConfig.IsError)
            {
                _logger.LogWarning($"error geting file config, errors are {string.Join(",", fileConfig.Errors.Select(x => x.Message))}");
                return;
            }

            var asJson = ToJson(fileConfig.Data);

            if(!fileConfig.IsError && asJson != _previousAsJson)
            {
                await _setter.Set(fileConfig.Data);
                _previousAsJson = asJson;
            }

            _logger.LogInformation("Finished polling consul");
        }

        /// <summary>
        /// We could do object comparison here but performance isnt really a problem. This might be an issue one day!
        /// </summary>
        /// <returns>hash of the config</returns>
        private string ToJson(FileConfiguration config)
        {
            var currentHash = JsonConvert.SerializeObject(config);
            return currentHash;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}

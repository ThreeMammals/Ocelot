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
        private IOcelotLogger _logger; 
        private IFileConfigurationRepository _repo;
        private IFileConfigurationSetter _setter;
        private string _previousHash;
        private Timer _timer;
        private bool _polling;

        public ConsulFileConfigurationPoller(IOcelotLoggerFactory factory, IFileConfigurationRepository repo, IFileConfigurationSetter setter)
        {
            _setter = setter;
            _logger = factory.CreateLogger<ConsulFileConfigurationPoller>();
            _repo = repo;
            _previousHash = "";
            _timer = new Timer(async x =>
            {
                if(_polling)
                {
                    return;
                }

                _polling = true;
                await Poll();
                _polling = false;

            }, null, 0, 1000);
        }
        
        private async Task Poll()
        {
            _logger.LogDebug("Started polling consul");

            var fileConfig = await _repo.Get();

            if(fileConfig.IsError)
            {
                _logger.LogDebug($"error geting file config, errors are {string.Join(",", fileConfig.Errors.Select(x => x.Message))}");
                return;
            }

            var hash = Hash(fileConfig.Data);

            if(!fileConfig.IsError && hash != _previousHash)
            {
                await _setter.Set(fileConfig.Data);
                _previousHash = hash;
            }

            _logger.LogDebug("Finished polling consul");
        }

        private string Hash(FileConfiguration config)
        {
            //todo - do something proper?
            var currentHash = JsonConvert.SerializeObject(config);
            return currentHash;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
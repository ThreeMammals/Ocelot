namespace Ocelot.Configuration.Repository
{
    public class ConsulFileConfigurationPollerOption : IFileConfigurationPollerOptions
    {
        private readonly IInternalConfigurationRepository _internalConfigRepo;
        private readonly IFileConfigurationRepository _fileConfigurationRepository;

        public ConsulFileConfigurationPollerOption(IInternalConfigurationRepository internalConfigurationRepository,
                                                   IFileConfigurationRepository fileConfigurationRepository)
        {
            _internalConfigRepo = internalConfigurationRepository;
            _fileConfigurationRepository = fileConfigurationRepository;
        }

        public int Delay => GetDelay().GetAwaiter().GetResult();

        private async Task<int> GetDelay()
        {
            var delay = 1000;
            try
            {
                var fileConfig = await _fileConfigurationRepository.GetAsync();
                var provider = fileConfig?.GlobalConfiguration?.ServiceDiscoveryProvider;
                if (provider != null && provider.PollingInterval > 0)
                {
                    delay = provider.PollingInterval;
                }
                else
                {
                    var internalConfig = _internalConfigRepo.Get();
                    var configuration = internalConfig?.Data?.ServiceProviderConfiguration;
                    if (configuration != null && configuration.PollingInterval > 0)
                    {
                        delay = configuration.PollingInterval;
                    }
                }
            }
            catch
            {
                delay = 0;
            }

            return delay;
        }
    }
}

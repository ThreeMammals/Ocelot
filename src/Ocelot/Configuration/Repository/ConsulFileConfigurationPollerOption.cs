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

        public int Delay => GetDelay();

        private int GetDelay()
        {
            var delay = 1000;

            var fileConfig = _fileConfigurationRepository.Get().GetAwaiter().GetResult(); // sync call, so TODO extend IFileConfigurationPollerOptions interface with 2nd async method
            if (fileConfig?.Data?.GlobalConfiguration?.ServiceDiscoveryProvider != null &&
                    !fileConfig.IsError &&
                    fileConfig.Data.GlobalConfiguration.ServiceDiscoveryProvider.PollingInterval > 0)
            {
                delay = fileConfig.Data.GlobalConfiguration.ServiceDiscoveryProvider.PollingInterval;
            }
            else
            {
                var internalConfig = _internalConfigRepo.Get();
                if (internalConfig?.Data?.ServiceProviderConfiguration != null &&
                !internalConfig.IsError &&
                internalConfig.Data.ServiceProviderConfiguration.PollingInterval > 0)
                {
                    delay = internalConfig.Data.ServiceProviderConfiguration.PollingInterval;
                }
            }

            return delay;
        }
    }
}

using Ocelot.Logging;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes
{
    public class PollKube : IServiceDiscoveryProvider, IDisposable
    {
        private readonly IOcelotLogger _logger;
        private readonly IServiceDiscoveryProvider _kubeServiceDiscoveryProvider;
        private readonly Timer _timer;
        
        private bool _polling;
        private List<Service> _services;

        public PollKube(int pollingInterval, IOcelotLoggerFactory factory, IServiceDiscoveryProvider kubeServiceDiscoveryProvider)
        {
            _logger = factory.CreateLogger<PollKube>();
            _kubeServiceDiscoveryProvider = kubeServiceDiscoveryProvider;
            _services = new List<Service>();

            _timer = new Timer(OnTimerCallbackAsync, null, pollingInterval, pollingInterval);
        }

        private async void OnTimerCallbackAsync(object state)
        {
            if (_polling)
            {
                return;
            }

            _polling = true;
            await Poll();
            _polling = false;
        }

        public Task<List<Service>> GetAsync()
        {
            return Task.FromResult(_services);
        }

        private async Task Poll()
        {
            _services = await _kubeServiceDiscoveryProvider.GetAsync();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}

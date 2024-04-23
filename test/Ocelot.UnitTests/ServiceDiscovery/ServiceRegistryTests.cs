using Ocelot.Values;

// nothing in use
namespace Ocelot.UnitTests.ServiceDiscovery
{
    public class ServiceRegistryTests : UnitTest
    {
        private Service _service;
        private List<Service> _services;
        private readonly ServiceRegistry _serviceRegistry;
        private readonly ServiceRepository _serviceRepository;

        public ServiceRegistryTests()
        {
            _serviceRepository = new ServiceRepository();
            _serviceRegistry = new ServiceRegistry(_serviceRepository);
        }

        [Fact]
        public void should_register_service()
        {
            this.Given(x => x.GivenAServiceToRegister("product", "localhost:5000", 80))
            .When(x => x.WhenIRegisterTheService())
            .Then(x => x.ThenTheServiceIsRegistered())
            .BDDfy();
        }

        [Fact]
        public void should_lookup_service()
        {
            this.Given(x => x.GivenAServiceIsRegistered("product", "localhost:600", 80))
            .When(x => x.WhenILookupTheService("product"))
            .Then(x => x.ThenTheServiceDetailsAreReturned())
            .BDDfy();
        }

        private void ThenTheServiceDetailsAreReturned()
        {
            _services[0].HostAndPort.DownstreamHost.ShouldBe(_service.HostAndPort.DownstreamHost);
            _services[0].HostAndPort.DownstreamPort.ShouldBe(_service.HostAndPort.DownstreamPort);
            _services[0].Name.ShouldBe(_service.Name);
        }

        private void WhenILookupTheService(string name)
        {
            _services = _serviceRegistry.Lookup(name);
        }

        private void GivenAServiceIsRegistered(string name, string address, int port)
        {
            _service = new Service(name, new ServiceHostAndPort(address, port), string.Empty, string.Empty, Array.Empty<string>());
            _serviceRepository.Set(_service);
        }

        private void GivenAServiceToRegister(string name, string address, int port)
        {
            _service = new Service(name, new ServiceHostAndPort(address, port), string.Empty, string.Empty, Array.Empty<string>());
        }

        private void WhenIRegisterTheService()
        {
            _serviceRegistry.Register(_service);
        }

        private void ThenTheServiceIsRegistered()
        {
            var serviceNameAndAddress = _serviceRepository.Get(_service.Name);
            serviceNameAndAddress[0].HostAndPort.DownstreamHost.ShouldBe(_service.HostAndPort.DownstreamHost);
            serviceNameAndAddress[0].HostAndPort.DownstreamPort.ShouldBe(_service.HostAndPort.DownstreamPort);
            serviceNameAndAddress[0].Name.ShouldBe(_service.Name);
        }
    }

    public interface IServiceRegistry
    {
        void Register(Service serviceNameAndAddress);

        List<Service> Lookup(string name);
    }

    public class ServiceRegistry : IServiceRegistry
    {
        private readonly IServiceRepository _repository;

        public ServiceRegistry(IServiceRepository repository)
        {
            _repository = repository;
        }

        public void Register(Service serviceNameAndAddress)
        {
            _repository.Set(serviceNameAndAddress);
        }

        public List<Service> Lookup(string name)
        {
            return _repository.Get(name);
        }
    }

    public interface IServiceRepository
    {
        List<Service> Get(string serviceName);

        void Set(Service serviceNameAndAddress);
    }

    public class ServiceRepository : IServiceRepository
    {
        private readonly Dictionary<string, List<Service>> _registeredServices;

        public ServiceRepository()
        {
            _registeredServices = new Dictionary<string, List<Service>>();
        }

        public List<Service> Get(string serviceName)
        {
            return _registeredServices[serviceName];
        }

        public void Set(Service serviceNameAndAddress)
        {
            if (_registeredServices.TryGetValue(serviceNameAndAddress.Name, out var services))
            {
                services.Add(serviceNameAndAddress);
                _registeredServices[serviceNameAndAddress.Name] = services;
            }
            else
            {
                _registeredServices[serviceNameAndAddress.Name] = new List<Service> { serviceNameAndAddress };
            }
        }
    }
}

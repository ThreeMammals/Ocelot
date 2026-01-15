using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceDiscovery;

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
    public void Should_register_service()
    {
        // Arrange
        _service = new Service("product", new ServiceHostAndPort("localhost:5000", 80), string.Empty, string.Empty, Array.Empty<string>());

        // Act
        _serviceRegistry.Register(_service);

        // Assert: Then The Service Is Registered
        var serviceNameAndAddress = _serviceRepository.Get(_service.Name);
        serviceNameAndAddress[0].HostAndPort.DownstreamHost.ShouldBe(_service.HostAndPort.DownstreamHost);
        serviceNameAndAddress[0].HostAndPort.DownstreamPort.ShouldBe(_service.HostAndPort.DownstreamPort);
        serviceNameAndAddress[0].Name.ShouldBe(_service.Name);
    }

    [Fact]
    public void Should_lookup_service()
    {
        // Arrange
        _service = new Service("product", new ServiceHostAndPort("localhost:600", 80), string.Empty, string.Empty, Array.Empty<string>());
        _serviceRepository.Set(_service);

        // Act
        _services = _serviceRegistry.Lookup("product");

        // Assert
        _services[0].HostAndPort.DownstreamHost.ShouldBe(_service.HostAndPort.DownstreamHost);
        _services[0].HostAndPort.DownstreamPort.ShouldBe(_service.HostAndPort.DownstreamPort);
        _services[0].Name.ShouldBe(_service.Name);
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
    public ServiceRegistry(IServiceRepository repository) => _repository = repository;
    public void Register(Service serviceNameAndAddress) => _repository.Set(serviceNameAndAddress);
    public List<Service> Lookup(string name) => _repository.Get(name);
}

public interface IServiceRepository
{
    List<Service> Get(string serviceName);
    void Set(Service serviceNameAndAddress);
}

public class ServiceRepository : IServiceRepository
{
    private readonly Dictionary<string, List<Service>> _registeredServices;
    public ServiceRepository() => _registeredServices = new Dictionary<string, List<Service>>();
    public List<Service> Get(string serviceName) => _registeredServices[serviceName];
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

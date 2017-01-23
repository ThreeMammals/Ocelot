using System.Collections.Generic;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class ServiceRegistryTests
    {
        private Service _service;
        private List<Service> _services;
        private ServiceRegistry _serviceRegistry;
        private ServiceRepository _serviceRepository;

        public ServiceRegistryTests()
        {
            _serviceRepository = new ServiceRepository();
            _serviceRegistry = new ServiceRegistry(_serviceRepository);
        }

        [Fact]
        public void should_register_service()
        {
            this.Given(x => x.GivenAServiceToRegister("product", "localhost:5000"))
            .When(x => x.WhenIRegisterTheService())
            .Then(x => x.ThenTheServiceIsRegistered())
            .BDDfy();
        }

        public void should_lookup_service()
        {
            this.Given(x => x.GivenAServiceIsRegistered("product", "localhost:600"))
            .When(x => x.WhenILookupTheService("product"))
            .Then(x => x.ThenTheServiceDetailsAreReturned())
            .BDDfy();
        }

        private void ThenTheServiceDetailsAreReturned()
        {
            _services[0].Address.ShouldBe(_service.Address);
            _services[0].Name.ShouldBe(_service.Name);
        }

        private void WhenILookupTheService(string name)
        {
            _services = _serviceRegistry.Lookup(name);
        }

        private void GivenAServiceIsRegistered(string name, string address)
        {
            _service = new Service(name, address);
            _serviceRepository.Set(_service);
        }

        private void GivenAServiceToRegister(string name, string address)
        {
            _service = new Service(name, address);
        }

        private void WhenIRegisterTheService()
        {
            _serviceRegistry.Register(_service);
        }

        private void ThenTheServiceIsRegistered()
        {
            var serviceNameAndAddress = _serviceRepository.Get(_service.Name);
            serviceNameAndAddress[0].Address.ShouldBe(_service.Address);
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

    public class Service
    {
        public Service(string name, string address)
        {
            Name = name;
            Address = address;
        }
        public string Name {get; private set;}
        public string Address {get; private set;}
    }

    public interface IServiceRepository
    {
        List<Service> Get(string serviceName);
        void Set(Service serviceNameAndAddress);
    }

    public class ServiceRepository : IServiceRepository
    {
        private Dictionary<string, List<Service>> _registeredServices;

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
            List<Service> services;
            if(_registeredServices.TryGetValue(serviceNameAndAddress.Name, out services))
            {
                services.Add(serviceNameAndAddress);
                _registeredServices[serviceNameAndAddress.Name] = services;
            }
            else
            {     
                _registeredServices[serviceNameAndAddress.Name] = new List<Service>(){ serviceNameAndAddress };
            }
            
        }
    }
}
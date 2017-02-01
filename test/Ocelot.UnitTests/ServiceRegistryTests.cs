using System.Collections.Generic;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using System;
using System.Linq;

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
            this.Given(x => x.GivenAServiceToRegister("product", "localhost:5000", 80))
            .When(x => x.WhenIRegisterTheService())
            .Then(x => x.ThenTheServiceIsRegistered())
            .BDDfy();
        }

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
            _service = new Service( Guid.NewGuid().ToString(), name,"", Enumerable.Empty<string>(), new HostAndPort(address, port));
            _serviceRepository.Set(_service);
        }

        private void GivenAServiceToRegister(string name, string address, int port)
        {
            _service = new Service(Guid.NewGuid().ToString(), name, "", Enumerable.Empty<string>(), new HostAndPort(address, port));
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
        List<Service> Lookup();
        List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> nameTagsPredicate,
            Predicate<Service> registryInformationPredicate);
        List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> predicate);
        List<Service> Lookup(Predicate<Service> predicate);
        List<Service> LookupAllServices();
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

      
        public List<Service> Lookup()
        {
            return _repository.GetAll();
        }

    
        public List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> nameTagsPredicate, Predicate<Service> registryInformationPredicate)
        {
            return _repository.GetServicesCatalog()
               .Where(kvp => nameTagsPredicate(kvp))
               .Select(kvp => kvp.Key)
               .Select(Lookup)
               .SelectMany(task => task)
               .Where(x => registryInformationPredicate(x))
               .ToList();
        }

        public List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> predicate)
        {
            return Lookup(nameTagsPredicate: predicate, registryInformationPredicate: x => true);
        }

        public List<Service> Lookup(Predicate<Service> predicate)
        {
            return Lookup(nameTagsPredicate: x => true, registryInformationPredicate:predicate);
        }

        public List<Service> LookupAllServices()
        {
            return _repository.GetAll();
        }
    }

    public class Service
    {
        public Service(string id, string name, string version, IEnumerable<string> tags, HostAndPort hostAndPort)
        {
            Name = name;
            Id = id;
            Version = version;
            Tags = tags;
            HostAndPort = hostAndPort;
        }

        public string Id { get;  private set; }

        public string Name {get; private set;}

        public string Version { get; private set; }

        public IEnumerable<string> Tags { get; private set; }

        public HostAndPort HostAndPort {get; private set;}
    }

    public interface IServiceRepository
    {
        List<Service> Get(string serviceName);
        List<Service> GetAll();
        Dictionary<string, string[]> GetServicesCatalog();
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

        public List<Service> GetAll()
        {
            List<Service> serviceInstances = new List<Service>();
            foreach (var servcies in _registeredServices.Values)
            {
                serviceInstances.AddRange(servcies);
            }
            return serviceInstances;
        }

        public Dictionary<string, string[]> GetServicesCatalog()
        {
            var results = GetAll();
            
            Dictionary<string, string[]> result = results
               .GroupBy(x => x.Name, x => x.Tags)
               .ToDictionary(g => g.Key, g => g.SelectMany(x => x ?? Enumerable.Empty<string>()).ToArray());

            return result;
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
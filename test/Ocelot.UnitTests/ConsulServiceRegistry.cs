using Consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Ocelot.UnitTests
{
    public class ConsulServiceRegistry : IServiceRegistry
    {
        private const string VERSION_PREFIX = "version-";
        private readonly ConsulRegistryConfiguration _configuration;
        private readonly ConsulClient _consul;

        public ConsulServiceRegistry(ConsulRegistryConfiguration configuration = null)
        {
            string consulHost = configuration?.HostName ?? "localhost";
            int consulPort = configuration?.Port ?? 8500;
            _configuration = new ConsulRegistryConfiguration { HostName = consulHost, Port = consulPort };

            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_configuration.HostName}:{_configuration.Port}");
            });
        }

        public List<Service> Lookup()
        {
            return Lookup(nameTagsPredicate: x => true, registryInformationPredicate: x => true);
        }

        public List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> predicate)
        {
            return Lookup(nameTagsPredicate: predicate, registryInformationPredicate: x => true);
        }

        public List<Service> Lookup(Predicate<Service> predicate)
        {
            return Lookup(nameTagsPredicate: x => true, registryInformationPredicate: predicate);
        }

        public List<Service> Lookup(string name)
        {
            var queryResult =  _consul.Health.Service(name, tag: "", passingOnly: true).Result;
            var instances = queryResult.Response.Select(serviceEntry => new Service(serviceEntry.Service.ID,
                serviceEntry.Service.Service,
                GetVersionFromStrings(serviceEntry.Service.Tags),
                serviceEntry.Service.Tags ?? Enumerable.Empty<string>(),new Values.HostAndPort(serviceEntry.Service.Address,
                serviceEntry.Service.Port)
           ));
            return instances.ToList();
        }

        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VERSION_PREFIX, StringComparison.Ordinal))
                .TrimStart(VERSION_PREFIX);
        }

        private IDictionary<string, string[]> GetServicesCatalog()
        {
            var queryResult = _consul.Catalog.Services().Result; // local agent datacenter is implied
            var services = queryResult.Response;
            return services;
        }
        
        public List<Service> Lookup(Predicate<KeyValuePair<string, string[]>> nameTagsPredicate, Predicate<Service> registryInformationPredicate)
        {
            return (GetServicesCatalog())
                .Where(kvp => nameTagsPredicate(kvp))
                .Select(kvp => kvp.Key)
                .Select(Lookup)
                .SelectMany(task => task)
                .Where(x => registryInformationPredicate(x))
                .ToList();
        }

        public List<Service> LookupAllServices()
        {
            var queryResult = _consul.Agent.Services().Result;
            var instances = queryResult.Response.Select(serviceEntry =>
               new Service(serviceEntry.Value.ID,
                serviceEntry.Value.Service,
                GetVersionFromStrings(serviceEntry.Value.Tags),
                serviceEntry.Value.Tags ?? Enumerable.Empty<string>(),
                new Values.HostAndPort(serviceEntry.Value.Address, serviceEntry.Value.Port)));

            return instances.ToList();
        }

        public void Register(Service serviceNameAndAddress)
        {
            var serviceId = GetServiceIdAsync(serviceNameAndAddress);

            string versionLabel = $"{VERSION_PREFIX}{serviceNameAndAddress.Version}";
            var tagList = (serviceNameAndAddress.Tags ?? Enumerable.Empty<string>()).ToList();
            tagList.Add(versionLabel);

            var registration = new AgentServiceRegistration
            {
                ID = serviceId,
                Name = serviceNameAndAddress.Name,
                Tags = tagList.ToArray(),
                Address = serviceNameAndAddress.HostAndPort.DownstreamHost,
                Port = serviceNameAndAddress.HostAndPort.DownstreamPort
             };

            _consul.Agent.ServiceRegister(registration); 
        }

        private string GetServiceIdAsync(Service serviceName)
        {
            return $"{serviceName.Name}_{serviceName.HostAndPort.DownstreamHost.Replace(".", "_")}_{serviceName.HostAndPort.DownstreamPort}";
        }

    }

    public static class StringExtensions
    {
        public static string TrimStart(this string source, string trim, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (source == null)
            {
                return null;
            }

            string s = source;
            while (s.StartsWith(trim, stringComparison))
            {
                s = s.Substring(trim.Length);
            }

            return s;
        }

    }
}

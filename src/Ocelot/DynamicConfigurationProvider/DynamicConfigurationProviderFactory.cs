using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Ocelot.Configuration;
using Ocelot.Logging;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DynamicConfigurationProvider
{
    public class DynamicConfigurationProviderFactory : IDynamicConfigurationProviderFactory
    {
        private readonly Dictionary<string, DynamicConfigurationProvider> _providers;
        private IOcelotLogger _logger;

        public DynamicConfigurationProviderFactory(IServiceProvider provider, IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<DynamicConfigurationProviderFactory>();

            var providers = provider.GetServices<DynamicConfigurationProvider>();
            try
            {
                _providers = providers.ToDictionary(x =>
                {
                    var storeAttribute = x.GetType().GetCustomAttribute<ConfigurationStoreAttribute>();

                    if (storeAttribute == null)
                    {
                        throw new InvalidOperationException($"{x.GetType().FullName} is missing {nameof(ConfigurationStoreAttribute)} attribute. For a class to be dynamic configuration provider, it must be decorated with {nameof(ConfigurationStoreAttribute)} attribute");
                    }

                    return storeAttribute.Store.ToString();
                });
            }
            catch (ArgumentException)
            {
                var duplicateStore = providers.GroupBy(x => x.GetType().GetCustomAttribute<ConfigurationStoreAttribute>().ToString()).FirstOrDefault(x => x.Count() > 1);
                throw new InvalidOperationException($"error while trying to add store {duplicateStore}. reason: duplicate store, same store already exists");
            }
        }

        public DynamicConfigurationProvider Get(IInternalConfiguration config)
        {
            if (config.DynamicReRouteConfiguration == null)
            {
                _logger.LogInformation($"Configuration section 'DynamicReRouteConfiguration' is not defined to ocelot");
            }
            else if (!_providers.ContainsKey(config.DynamicReRouteConfiguration.Store?.ToString()))
            {
                _logger.LogWarning($"Provider for store '{config.DynamicReRouteConfiguration.Store?.ToString()}' could not found or is not registered.");
            }
            else
            {
                _logger.LogInformation($"Using store '{config.DynamicReRouteConfiguration.Store.ToString()}' to get Dynamic configuration.");
                return _providers[config.DynamicReRouteConfiguration.Store.ToString()];
            }
            
            return null;
        }
    }
}

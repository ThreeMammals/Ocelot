using Ocelot.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.DynamicConfigurationProvider
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ConfigurationStoreAttribute : Attribute
    {
        public string Store { get; private set; }

        public ConfigurationStoreAttribute(string store)
        {
            if (string.IsNullOrWhiteSpace(store))
            {
                throw new ArgumentNullException("invalid value for 'store'. It cannot be null, empty or whitespace.");
            }

            this.Store = store;
        }
    }
}

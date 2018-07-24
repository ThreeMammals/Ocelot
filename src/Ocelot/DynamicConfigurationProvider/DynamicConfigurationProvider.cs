using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.DynamicConfigurationProvider
{
    public abstract class DynamicConfigurationProvider
    {
        protected readonly IOcelotLogger _logger;

        public DynamicConfigurationProvider(IOcelotLogger logger)
        {
            _logger = logger;
        }

        public async Task<FileReRoute> BuildRouteConfigurationAsync(string host, string port, string key)
        {
            //using stopwatch to log time taken by implementation of GetRouteConfigurationAsync
            //and this gives idea about the latency while fetching dynamic configuration
            Stopwatch stopwatch = new Stopwatch();

            _logger.LogDebug($"Getting route configuration from {host}:{port} for key:{key} using {this.GetType().Name}");

            stopwatch.Start();
            var reRoute = await GetRouteConfigurationAsync(host, port, key);
            stopwatch.Stop();

            _logger.LogDebug($"Completed route configuration fetch in {stopwatch.ElapsedMilliseconds}ms");

            return reRoute;
        }

        protected abstract Task<FileReRoute> GetRouteConfigurationAsync(string host, string port, string key);
    }
}

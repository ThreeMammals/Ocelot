using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Responses;
using FileSys = System.IO.File;

namespace Ocelot.Configuration.Repository
{
    public class DiskFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;
        private readonly IOcelotCache<FileConfiguration> _cache;

        private FileInfo _ocelotFile;
        private FileInfo _environmentFile;
        private readonly object _lock = new();
        public const string CacheKey = nameof(DiskFileConfigurationRepository);

        public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment,
            IOcelotConfigurationChangeTokenSource changeTokenSource,
            IOcelotCache<FileConfiguration> cache)
        {
            _hostingEnvironment = hostingEnvironment;
            _changeTokenSource = changeTokenSource;
            _cache = cache;
            Initialize(AppContext.BaseDirectory);
        }

        public DiskFileConfigurationRepository(IWebHostEnvironment hostingEnvironment,
            IOcelotConfigurationChangeTokenSource changeTokenSource,
            IOcelotCache<FileConfiguration> cache,
            string folder)
        {
            _hostingEnvironment = hostingEnvironment;
            _changeTokenSource = changeTokenSource;
            _cache = cache;
            Initialize(folder);
        }

        private void Initialize(string folder)
        {
            folder ??= AppContext.BaseDirectory;
            _ocelotFile = new FileInfo(Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile));
            var envFile = !string.IsNullOrEmpty(_hostingEnvironment.EnvironmentName)
                ? string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, _hostingEnvironment.EnvironmentName)
                : ConfigurationBuilderExtensions.PrimaryConfigFile;
            _environmentFile = new FileInfo(Path.Combine(folder, envFile));
        }

        public Task<Response<FileConfiguration>> Get()
        {
            var configuration = _cache.Get(CacheKey, region: CacheKey);
            if (configuration != null)
            {
                return Task.FromResult<Response<FileConfiguration>>(new OkResponse<FileConfiguration>(configuration));
            }

            string jsonConfiguration;
            lock (_lock)
            {
                jsonConfiguration = FileSys.ReadAllText(_environmentFile.FullName);
            }

            var fileConfiguration = JsonConvert.DeserializeObject<FileConfiguration>(jsonConfiguration);
            return Task.FromResult<Response<FileConfiguration>>(new OkResponse<FileConfiguration>(fileConfiguration));
        }

        /// <summary>Default TTL in seconds for caching <see cref="FileConfiguration"/> in the <see cref="Set(FileConfiguration)"/> method.</summary>
        /// <value>An <see cref="int"/> value, 300 seconds (5 minutes) by default.</value>
        public static int CacheTtlSeconds { get; set; } = 300;

        public Task<Response> Set(FileConfiguration fileConfiguration)
        {
            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);
            lock (_lock)
            {
                if (_environmentFile.Exists)
                {
                    _environmentFile.Delete();
                }

                FileSys.WriteAllText(_environmentFile.FullName, jsonConfiguration);

                if (_ocelotFile.Exists)
                {
                    _ocelotFile.Delete();
                }

                FileSys.WriteAllText(_ocelotFile.FullName, jsonConfiguration);
            }

            _changeTokenSource.Activate();
            _cache.AddAndDelete(CacheKey, fileConfiguration, TimeSpan.FromSeconds(CacheTtlSeconds), region: CacheKey);
            return Task.FromResult<Response>(new OkResponse());
        }
    }
}

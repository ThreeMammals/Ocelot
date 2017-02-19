using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Ocelot.Services;
using Newtonsoft.Json;
using System.IO;

namespace Ocelot.UnitTests.Services
{
    public class GetFileConfigurationTests
    {
        private readonly IGetFileConfiguration _getReRoutes;
        private FileConfiguration _result;

        public GetFileConfigurationTests()
        {
            _getReRoutes = new GetFileConfiguration();
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var reRoutes = new List<FileReRoute>
            {
                new FileReRoute
                {
                    DownstreamHost = "localhost",
                    DownstreamPort = 80,
                    DownstreamScheme = "https",
                    DownstreamPathTemplate = "/test/test/{test}"
                }
            };

            var globalConfiguration = new FileGlobalConfiguration
            {
                AdministrationPath = "testy",
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Provider = "consul",
                    Port = 198,
                    Host = "blah"
                }
            };

            var config = new FileConfiguration();
            config.GlobalConfiguration = globalConfiguration;
            config.ReRoutes.AddRange(reRoutes);

            this.Given(x => x.GivenTheConfigurationIs(config))
                .When(x => x.WhenIGetTheReRoutes())
                .Then(x => x.ThenTheFollowingIsReturned(config))
                .BDDfy();
        }

        private void GivenTheConfigurationIs(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{AppContext.BaseDirectory}/configuration.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }

        private void WhenIGetTheReRoutes()
        {
            _result = _getReRoutes.Invoke().Data;
        }

        private void ThenTheFollowingIsReturned(FileConfiguration expected)
        {
            _result.GlobalConfiguration.AdministrationPath.ShouldBe(expected.GlobalConfiguration.AdministrationPath);
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expected.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Port);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Provider.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Provider);

            for(var i = 0; i < _result.ReRoutes.Count; i++)
            {
                _result.ReRoutes[i].DownstreamHost.ShouldBe(expected.ReRoutes[i].DownstreamHost);
                _result.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expected.ReRoutes[i].DownstreamPathTemplate);
                _result.ReRoutes[i].DownstreamPort.ShouldBe(expected.ReRoutes[i].DownstreamPort);
                _result.ReRoutes[i].DownstreamScheme.ShouldBe(expected.ReRoutes[i].DownstreamScheme);
            }
        }
    }
}
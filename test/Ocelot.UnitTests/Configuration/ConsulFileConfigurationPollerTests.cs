using System;
using System.Collections.Generic;
using System.Diagnostics;
using Moq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Logging;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;
using Shouldly;
using static Ocelot.UnitTests.Wait;


namespace Ocelot.UnitTests.Configuration
{
    public class ConsulFileConfigurationPollerTests : IDisposable
    {
        private ConsulFileConfigurationPoller _poller;
        private Mock<IOcelotLoggerFactory> _factory;
        private Mock<IFileConfigurationRepository> _repo;
        private Mock<IFileConfigurationSetter> _setter;
        private FileConfiguration _fileConfig;

        public ConsulFileConfigurationPollerTests()
        {
            var logger = new Mock<IOcelotLogger>();
            _factory = new Mock<IOcelotLoggerFactory>();
            _factory.Setup(x => x.CreateLogger<ConsulFileConfigurationPoller>()).Returns(logger.Object);
            _repo = new Mock<IFileConfigurationRepository>();
            _setter = new Mock<IFileConfigurationSetter>();
            _fileConfig = new FileConfiguration();
            _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(_fileConfig));
            _poller = new ConsulFileConfigurationPoller(_factory.Object, _repo.Object, _setter.Object);
        }
        public void Dispose()
        {
            _poller.Dispose();
        }

        [Fact]
        public void should_start()
        {
           this.Given(x => ThenTheSetterIsCalled(_fileConfig))
                .BDDfy();
        }

        [Fact]
        public void should_call_setter_when_gets_new_config()
        {

            var newConfig = new FileConfiguration {
                ReRoutes = new List<FileReRoute>
                {   
                    new FileReRoute
                    {
                        DownstreamHost = "test"
                    }
                }
            };

            this.Given(x => WhenTheConfigIsChangedInConsul(newConfig))
                .Then(x => ThenTheSetterIsCalled(newConfig))
                .BDDfy();
        }

        private void WhenTheConfigIsChangedInConsul(FileConfiguration newConfig)
        {
            _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(newConfig));
        }

        private void ThenTheSetterIsCalled(FileConfiguration fileConfig)
        {
            var result = WaitFor(2000).Until(() => {
                try
                {
                    _setter.Verify(x => x.Set(fileConfig), Times.Once);
                    return true;
                }
                catch(Exception ex)
                {
                    return false;
                }
            });
            result.ShouldBeTrue();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using TestStack.BDDfy;
using Xunit;
using Shouldly;
using static Ocelot.Infrastructure.Wait;

namespace Ocelot.UnitTests.Configuration
{
    public class ConsulFileConfigurationPollerTests : IDisposable
    {
        private readonly ConsulFileConfigurationPoller _poller;
        private Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IFileConfigurationRepository> _repo;
        private readonly Mock<IFileConfigurationSetter> _setter;
        private readonly FileConfiguration _fileConfig;
        private Mock<IConsulPollerConfiguration> _config;

        public ConsulFileConfigurationPollerTests()
        {
            var logger = new Mock<IOcelotLogger>();
            _factory = new Mock<IOcelotLoggerFactory>();
            _factory.Setup(x => x.CreateLogger<ConsulFileConfigurationPoller>()).Returns(logger.Object);
            _repo = new Mock<IFileConfigurationRepository>();
            _setter = new Mock<IFileConfigurationSetter>();
            _fileConfig = new FileConfiguration();
            _config = new Mock<IConsulPollerConfiguration>();
            _repo.Setup(x => x.Get()).ReturnsAsync(new OkResponse<FileConfiguration>(_fileConfig));
            _config.Setup(x => x.Delay).Returns(100);
            _poller = new ConsulFileConfigurationPoller(_factory.Object, _repo.Object, _setter.Object, _config.Object);
        }
        
        public void Dispose()
        {
            _poller.Dispose();
        }

        [Fact]
        public void should_start()
        {
           this.Given(x => ThenTheSetterIsCalled(_fileConfig, 1))
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
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "test"
                            }
                        },
                    }
                }
            };

            this.Given(x => WhenTheConfigIsChangedInConsul(newConfig, 0))
                .Then(x => ThenTheSetterIsCalledAtLeast(newConfig, 1))
                .BDDfy();
        }

        [Fact]
        public void should_not_poll_if_already_polling()
        {
            var newConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "test"
                            }
                        },
                    }
                }
            };

            this.Given(x => WhenTheConfigIsChangedInConsul(newConfig, 10))
                .Then(x => ThenTheSetterIsCalled(newConfig, 1))
                .BDDfy();
        }

        [Fact]
        public void should_do_nothing_if_call_to_consul_fails()
        {
            var newConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "test"
                            }
                        },
                    }
                }
            };

            this.Given(x => WhenConsulErrors())
                .Then(x => ThenTheSetterIsCalled(newConfig, 0))
                .BDDfy();
        }

        private void WhenConsulErrors()
        {
            _repo
                .Setup(x => x.Get())
                .ReturnsAsync(new ErrorResponse<FileConfiguration>(new AnyError()));
        }

        private void WhenTheConfigIsChangedInConsul(FileConfiguration newConfig, int delay)
        {
            _repo
                .Setup(x => x.Get())
                .Callback(() => Thread.Sleep(delay))
                .ReturnsAsync(new OkResponse<FileConfiguration>(newConfig));
        }

        private void ThenTheSetterIsCalled(FileConfiguration fileConfig, int times)
        {
            var result = WaitFor(2000).Until(() => {
                try
                {
                    _setter.Verify(x => x.Set(fileConfig), Times.Exactly(times));
                    return true;
                }
                catch(Exception)
                {
                    return false;
                }
            });
            result.ShouldBeTrue();
        }

        private void ThenTheSetterIsCalledAtLeast(FileConfiguration fileConfig, int times)
        {
            var result = WaitFor(2000).Until(() => {
                try
                {
                    _setter.Verify(x => x.Set(fileConfig), Times.AtLeast(times));
                    return true;
                }
                catch(Exception)
                {
                    return false;
                }
            });
            result.ShouldBeTrue();
        }
    }
}

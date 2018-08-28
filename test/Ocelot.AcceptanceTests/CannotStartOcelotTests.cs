namespace Ocelot.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using Ocelot.Configuration.File;
    using Shouldly;
    using Xunit;

    public class CannotStartOcelotTests : IDisposable
    {
        private readonly Steps _steps;

        public CannotStartOcelotTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_globally()
        {
            var invalidConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 51878,
                            }
                        },
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        Key = "Laura",
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    QoSOptions = new FileQoSOptions
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1
                    }
                }
            };

            Exception exception = null;
            _steps.GivenThereIsAConfiguration(invalidConfig);
            try
            {
                _steps.GivenOcelotIsRunning();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            exception.ShouldNotBeNull();
            exception.Message.ShouldBe("One or more errors occurred. (Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?)");
        }

        [Fact]
        public void should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_for_re_route()
        {
            var invalidConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 51878,
                            }
                        },
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        Key = "Laura",
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            ExceptionsAllowedBeforeBreaking = 1
                        }
                    }
                }
            };

            Exception exception = null;
            _steps.GivenThereIsAConfiguration(invalidConfig);
            try
            {
                _steps.GivenOcelotIsRunning();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            exception.ShouldNotBeNull();
            exception.Message.ShouldBe("One or more errors occurred. (Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?)");
        }

        [Fact]
        public void should_throw_exception_if_cannot_start()
        {
            var invalidConfig = new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "api",
                        DownstreamPathTemplate = "test"
                    }
                }
            };

            Exception exception = null;
            _steps.GivenThereIsAConfiguration(invalidConfig);
            try
            {
                _steps.GivenOcelotIsRunning();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            exception.ShouldNotBeNull();
            exception.Message.ShouldBe("One or more errors occurred. (Unable to start Ocelot, errors are: Downstream Path Template test doesnt start with forward slash,Upstream Path Template api doesnt start with forward slash,When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!)");
        }

        public void Dispose()
        {
            _steps.Dispose();
        }
    }
}

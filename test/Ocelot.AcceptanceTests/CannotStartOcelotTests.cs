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
            catch(Exception ex)
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

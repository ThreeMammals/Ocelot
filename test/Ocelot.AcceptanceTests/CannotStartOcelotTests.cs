using System;
using System.Collections.Generic;
using Ocelot.Configuration.File;
using Shouldly;
using Xunit;

namespace Ocelot.AcceptanceTests
{
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
        }
        
        public void Dispose()
        {
            _steps.Dispose();
        }
    }
}

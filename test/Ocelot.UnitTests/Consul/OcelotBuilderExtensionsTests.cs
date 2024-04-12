using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using System.Reflection;

namespace Ocelot.UnitTests.Consul
{
    public class OcelotBuilderExtensionsTests : UnitTest
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private Exception _ex;

        public OcelotBuilderExtensionsTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton(GetHostingEnvironment());
            _services.AddSingleton(_configRoot);
        }

        private static IWebHostEnvironment GetHostingEnvironment()
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment
                .Setup(e => e.ApplicationName)
                .Returns(typeof(OcelotBuilderExtensionsTests).GetTypeInfo().Assembly.GetName().Name);

            return environment.Object;
        }

        [Fact]
        public void should_set_up_consul()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpConsul())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        private void WhenISetUpOcelotServices()
        {
            try
            {
                _ocelotBuilder = _services.AddOcelot(_configRoot);
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenISetUpConsul()
        {
            try
            {
                _ocelotBuilder.AddConsul().AddConfigStoredInConsul();
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void ThenAnExceptionIsntThrown()
        {
            _ex.ShouldBeNull();
        }
    }
}

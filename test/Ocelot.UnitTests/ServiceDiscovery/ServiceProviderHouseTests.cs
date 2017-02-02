using System;
using System.Collections.Generic;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class ServiceProviderHouseTests
    {
        private Ocelot.ServiceDiscovery.IServiceProvider _serviceProvider;
        private readonly ServiceProviderHouse _serviceProviderHouse;
        private Response _addResult;
        private Response<Ocelot.ServiceDiscovery.IServiceProvider> _getResult;
        private string _key;

        public ServiceProviderHouseTests()
        {
            _serviceProviderHouse = new ServiceProviderHouse();
        }

        [Fact]
        public void should_store_service_provider()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsAServiceProvider(key, new FakeServiceProvider()))
                .When(x => x.WhenIAddTheServiceProvider())
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_get_service_provider()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsAServiceProvider(key, new FakeServiceProvider()))
                .When(x => x.WhenWeGetTheServiceProvider(key))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_service_providers_by_key()
        {
            var key = "test";
            var keyTwo = "testTwo";

            this.Given(x => x.GivenThereIsAServiceProvider(key, new FakeServiceProvider()))
                .And(x => x.GivenThereIsAServiceProvider(keyTwo, new FakeConsulServiceProvider()))
                .When(x => x.WhenWeGetTheServiceProvider(key))
                .Then(x => x.ThenTheServiceProviderIs<FakeServiceProvider>())
                .When(x => x.WhenWeGetTheServiceProvider(keyTwo))
                .Then(x => x.ThenTheServiceProviderIs<FakeConsulServiceProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_no_service_provider_house_with_key()
        {
            this.When(x => x.WhenWeGetTheServiceProvider("test"))
            .Then(x => x.ThenAnErrorIsReturned())
            .BDDfy();
        }

        private void ThenAnErrorIsReturned()
        {
            _getResult.IsError.ShouldBeTrue();
            _getResult.Errors[0].ShouldBeOfType<UnableToFindServiceProviderError>();
        }

        private void ThenTheServiceProviderIs<T>()
        {
            _getResult.Data.ShouldBeOfType<T>();
        }

        private void ThenItIsAdded()
        {
            _addResult.IsError.ShouldBe(false);
            _addResult.ShouldBeOfType<OkResponse>();
        }

        private void WhenIAddTheServiceProvider()
        {
            _addResult = _serviceProviderHouse.Add(_key, _serviceProvider);
        }

        private void GivenThereIsAServiceProvider(string key, Ocelot.ServiceDiscovery.IServiceProvider serviceProvider)
        {
            _key = key;
            _serviceProvider = serviceProvider;
            WhenIAddTheServiceProvider();
        }

        private void WhenWeGetTheServiceProvider(string key)
        {
            _getResult = _serviceProviderHouse.Get(key);
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_serviceProvider);
        }

        class FakeServiceProvider : Ocelot.ServiceDiscovery.IServiceProvider
        {
          public List<Service> Get()
            {
                throw new NotImplementedException();
            }
        }

        class FakeConsulServiceProvider : Ocelot.ServiceDiscovery.IServiceProvider
        {
            public List<Service> Get()
            {
                throw new NotImplementedException();
            }
        }
    }
}

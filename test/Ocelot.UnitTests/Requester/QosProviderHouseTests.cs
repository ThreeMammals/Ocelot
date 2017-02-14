using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class QosProviderHouseTests
    {
        private IQoSProvider _qoSProvider;
        private readonly QosProviderHouse _qosProviderHouse;
        private Response _addResult;
        private Response<IQoSProvider> _getResult;
        private string _key;

        public QosProviderHouseTests()
        {
            _qosProviderHouse = new QosProviderHouse();
        }

        [Fact]
        public void should_store_qos_provider()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsAQoSProvider(key, new FakeQoSProvider()))
                .When(x => x.WhenIAddTheQoSProvider())
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_get_qos_provider()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsAQoSProvider(key, new FakeQoSProvider()))
                .When(x => x.WhenWeGetTheQoSProvider(key))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_qos_providers_by_key()
        {
            var key = "test";
            var keyTwo = "testTwo";

            this.Given(x => x.GivenThereIsAQoSProvider(key, new FakeQoSProvider()))
                .And(x => x.GivenThereIsAQoSProvider(keyTwo, new FakePollyQoSProvider()))
                .When(x => x.WhenWeGetTheQoSProvider(key))
                .Then(x => x.ThenTheQoSProviderIs<FakeQoSProvider>())
                .When(x => x.WhenWeGetTheQoSProvider(keyTwo))
                .Then(x => x.ThenTheQoSProviderIs<FakePollyQoSProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_no_qos_provider_with_key()
        {
            this.When(x => x.WhenWeGetTheQoSProvider("test"))
            .Then(x => x.ThenAnErrorIsReturned())
            .BDDfy();
        }

        private void ThenAnErrorIsReturned()
        {
            _getResult.IsError.ShouldBeTrue();
            _getResult.Errors[0].ShouldBeOfType<UnableToFindQoSProviderError>();
        }

        private void ThenTheQoSProviderIs<T>()
        {
            _getResult.Data.ShouldBeOfType<T>();
        }

        private void ThenItIsAdded()
        {
            _addResult.IsError.ShouldBe(false);
            _addResult.ShouldBeOfType<OkResponse>();
        }

        private void WhenIAddTheQoSProvider()
        {
            _addResult = _qosProviderHouse.Add(_key, _qoSProvider);
        }


        private void GivenThereIsAQoSProvider(string key, IQoSProvider qoSProvider)
        {
            _key = key;
            _qoSProvider = qoSProvider;
            WhenIAddTheQoSProvider();
        }

        private void WhenWeGetTheQoSProvider(string key)
        {
            _getResult = _qosProviderHouse.Get(key);
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_qoSProvider);
        }

        class FakeQoSProvider : IQoSProvider
        {
            public CircuitBreaker CircuitBreaker { get; }
        }

        class FakePollyQoSProvider : IQoSProvider
        {
            public CircuitBreaker CircuitBreaker { get; }
        }
    }
}

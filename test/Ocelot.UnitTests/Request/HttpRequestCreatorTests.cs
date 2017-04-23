namespace Ocelot.UnitTests.Request
{
    using System.Net.Http;

    using Ocelot.Request.Builder;
    using Ocelot.Requester.QoS;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpRequestCreatorTests
    {
        private readonly IRequestCreator _requestCreator;
        private readonly bool _isQos;
        private readonly IQoSProvider _qoSProvider;
        private readonly HttpRequestMessage _requestMessage;
        private Response<Ocelot.Request.Request> _response;

        public HttpRequestCreatorTests()
        {
            _requestCreator = new HttpRequestCreator();
            _isQos = true;
            _qoSProvider = new NoQoSProvider();
            _requestMessage = new HttpRequestMessage();
        }

        [Fact]
        public void ShouldBuildRequest()
        {
            this.When(x => x.WhenIBuildARequest())
                .Then(x => x.ThenTheRequestContainsTheRequestMessage())
                .BDDfy();
        }

        private void WhenIBuildARequest()
        {
            _response = _requestCreator.Build(_requestMessage, _isQos, _qoSProvider).GetAwaiter().GetResult();
        }

        private void ThenTheRequestContainsTheRequestMessage()
        {
            _response.Data.HttpRequestMessage.ShouldBe(_requestMessage);
        }

        private void ThenTheRequestContainsTheIsQos()
        {
            _response.Data.IsQos.ShouldBe(_isQos);
        }

        private void ThenTheRequestContainsTheQosProvider()
        {
            _response.Data.QosProvider.ShouldBe(_qoSProvider);
        }
    }
}

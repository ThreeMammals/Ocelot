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
        private readonly bool _useCookieContainer;
        private readonly bool _allowAutoRedirect;
        private Response<Ocelot.Request.Request> _response;
        private string _reRouteKey;
        private readonly bool _useTracing;

        public HttpRequestCreatorTests()
        {
            _requestCreator = new HttpRequestCreator();
            _isQos = true;
            _qoSProvider = new NoQoSProvider();
            _useCookieContainer = false;
            _allowAutoRedirect = false;

            _requestMessage = new HttpRequestMessage();
        }

        [Fact]
        public void ShouldBuildRequest()
        {
            this.When(x => x.WhenIBuildARequest())
                .Then(x => x.ThenTheRequestContainsTheRequestMessage())
                .Then(x => x.ThenTheRequestContainsTheIsQos())
                .Then(x => x.ThenTheRequestContainsTheQosProvider())
                .Then(x => x.ThenTheRequestContainsUseCookieContainer())
                .Then(x => x.ThenTheRequestContainsUseTracing())
                .Then(x => x.ThenTheRequestContainsAllowAutoRedirect())
                .BDDfy();
        }

        private void WhenIBuildARequest()
        {
            _response = _requestCreator.Build(_requestMessage,
                    _isQos, _qoSProvider, _useCookieContainer, _allowAutoRedirect, _reRouteKey, _useTracing)
                .GetAwaiter()
                .GetResult();
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

        private void ThenTheRequestContainsUseCookieContainer()
        {
            _response.Data.UseCookieContainer.ShouldBe(_useCookieContainer);
        }

        private void ThenTheRequestContainsUseTracing()
        {
            _response.Data.IsTracing.ShouldBe(_useTracing);
        }

        private void ThenTheRequestContainsAllowAutoRedirect()
        {
            _response.Data.AllowAutoRedirect.ShouldBe(_allowAutoRedirect);
        }
    }
}

namespace Ocelot.UnitTests.Configuration
{
    using Moq;
    using Ocelot.Cache;
    using Ocelot.Configuration.Creator;
    using Xunit;

    public class ReRoutesCreatorTests
    {
        private ReRoutesCreator _creator;
        private Mock<IClaimsToThingCreator> _cthCreator;
        private Mock<IAuthenticationOptionsCreator> _aoCreator;
        private Mock<IUpstreamTemplatePatternCreator> _utpCreator;
        private Mock<IRequestIdKeyCreator> _ridkCreator;
        private Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private Mock<IReRouteOptionsCreator> _rroCreator;
        private Mock<IRateLimitOptionsCreator> _rloCreator;
        private Mock<IRegionCreator> _rCreator;
        private Mock<IHttpHandlerOptionsCreator> _hhoCreator;
        private Mock<IHeaderFindAndReplaceCreator> _hfarCreator;
        private Mock<IDownstreamAddressesCreator> _daCreator;
        private Mock<ILoadBalancerOptionsCreator> _lboCreator;

        public ReRoutesCreatorTests()
        {
            _cthCreator = new Mock<IClaimsToThingCreator>();
            _aoCreator = new Mock<IAuthenticationOptionsCreator>();
            _utpCreator = new Mock<IUpstreamTemplatePatternCreator>();
            _ridkCreator = new Mock<IRequestIdKeyCreator>();
            _qosOptionsCreator = new Mock<IQoSOptionsCreator>();
            _rroCreator = new Mock<IReRouteOptionsCreator>();
            _rloCreator = new Mock<IRateLimitOptionsCreator>();
            _rCreator = new Mock<IRegionCreator>();
            _hhoCreator = new Mock<IHttpHandlerOptionsCreator>();
            _hfarCreator = new Mock<IHeaderFindAndReplaceCreator>();
            _daCreator = new Mock<IDownstreamAddressesCreator>();
            _lboCreator = new Mock<ILoadBalancerOptionsCreator>();

            _creator = new ReRoutesCreator(
                _cthCreator.Object, 
                _aoCreator.Object,
                _utpCreator.Object,
                _ridkCreator.Object,
                _qosOptionsCreator.Object,
                _rroCreator.Object,
                _rloCreator.Object,
                _rCreator.Object,
                _hhoCreator.Object,
                _hfarCreator.Object,
                _daCreator.Object,
                _lboCreator.Object
                );
        }

        [Fact]
        public void should_do()
        {

        }
    }
}

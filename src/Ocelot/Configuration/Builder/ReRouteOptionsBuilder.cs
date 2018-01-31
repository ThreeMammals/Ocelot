namespace Ocelot.Configuration.Builder
{
    public class ReRouteOptionsBuilder
    {
        private bool _isAuthenticated;
        private bool _isAuthorised;
        private bool _isCached;
        private bool _isQoS;
        private bool _enableRateLimiting;

        public ReRouteOptionsBuilder WithIsCached(bool isCached)
        {
            _isCached = isCached;
            return this;
        }

        public ReRouteOptionsBuilder WithIsAuthenticated(bool isAuthenticated)
        {
            _isAuthenticated = isAuthenticated;
            return this;
        }

        public ReRouteOptionsBuilder WithIsAuthorised(bool isAuthorised)
        {
            _isAuthorised = isAuthorised;
            return this;
        }

        public ReRouteOptionsBuilder WithIsQos(bool isQoS)
        {
            _isQoS = isQoS;
            return this;
        }

        public ReRouteOptionsBuilder WithRateLimiting(bool enableRateLimiting)
        {
            _enableRateLimiting = enableRateLimiting;
            return this;
        }

        public ReRouteOptions Build()
        {
            return new ReRouteOptions(_isAuthenticated, _isAuthorised, _isCached, _isQoS, _enableRateLimiting);
        }
    }
}
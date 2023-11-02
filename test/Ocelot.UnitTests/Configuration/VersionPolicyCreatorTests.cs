using Ocelot.Configuration.Creator;

namespace Ocelot.UnitTests.Configuration
{
    public class VersionPolicyCreatorTests
    {
        private readonly VersionPolicyCreator _creator;
        private string _input;
        private HttpVersionPolicy _result;

        public VersionPolicyCreatorTests()
        {
            _creator = new VersionPolicyCreator();
        }

        [Theory]
        [InlineData(VersionPolicies.Downgradable)]
        [InlineData(VersionPolicies.Exact)]
        [InlineData(VersionPolicies.Upgradeable)]
        public void should_create_version_policy_based_on_input(string versionPolicy)
        {
            this.Given(_ => GivenTheInput(versionPolicy))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs(versionPolicy))
                .BDDfy();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("invalid version")]
        public void should_default_to_request_version_or_lower(string versionPolicy)
        {
            this.Given(_ => GivenTheInput(versionPolicy))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs(HttpVersionPolicy.RequestVersionOrLower))
                .BDDfy();
        }

        private void GivenTheInput(string input)
        {
            _input = input;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_input);
        }

        private void ThenTheResultIs(HttpVersionPolicy result)
        {
            _result.ShouldBe(result);
        }
        
        private void ThenTheResultIs(string result)
        {
            _result.ShouldBe(result switch
            {
                VersionPolicies.Upgradeable => HttpVersionPolicy.RequestVersionOrHigher,
                VersionPolicies.Exact => HttpVersionPolicy.RequestVersionExact,
                VersionPolicies.Downgradable => HttpVersionPolicy.RequestVersionOrLower,
                _ => HttpVersionPolicy.RequestVersionOrLower,
            });
        }
    }
}

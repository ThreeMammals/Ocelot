using Ocelot.Configuration.Creator;

namespace Ocelot.UnitTests.Configuration
{
    public class HttpVersionPolicyCreatorTests
    {
        private readonly HttpVersionPolicyCreator _creator;
        private string _input;
        private HttpVersionPolicy _result;

        public HttpVersionPolicyCreatorTests()
        {
            _creator = new HttpVersionPolicyCreator();
        }

        [Theory]
        [InlineData(VersionPolicies.RequestVersionOrLower)]
        [InlineData(VersionPolicies.RequestVersionExact)]
        [InlineData(VersionPolicies.RequestVersionOrHigher)]
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

        [Fact]
        public void should_default_to_request_version_or_lower_when_setting_gibberish()
        {
            this.Given(_ => GivenTheInput("string is gibberish"))
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
                VersionPolicies.RequestVersionOrHigher => HttpVersionPolicy.RequestVersionOrHigher,
                VersionPolicies.RequestVersionExact => HttpVersionPolicy.RequestVersionExact,
                VersionPolicies.RequestVersionOrLower => HttpVersionPolicy.RequestVersionOrLower,
                _ => HttpVersionPolicy.RequestVersionOrLower,
            });
        }
    }
}

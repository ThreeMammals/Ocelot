using Ocelot.Configuration.Authentication;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class HashMatcherTests
    {
        private string _password;
        private string _hash;
        private string _salt;
        private bool _result;
        private HashMatcher _hashMatcher;

        public HashMatcherTests()
        {
            _hashMatcher = new HashMatcher();
        }

        [Fact]
        public void should_match_hash()
        {   
            var hash = "kE/mxd1hO9h9Sl2VhGhwJUd9xZEv4NP6qXoN39nIqM4=";
            var salt = "zzWITpnDximUNKYLiUam/w==";
            var password = "secret";

            this.Given(x => GivenThePassword(password))
                .And(x => GivenTheHash(hash))
                .And(x => GivenTheSalt(salt))
                .When(x => WhenIMatch())
                .Then(x => ThenTheResultIs(true))
                .BDDfy();
        }

        [Fact]
        public void should_not_match_hash()
        {
            var hash = "kE/mxd1hO9h9Sl2VhGhwJUd9xZEv4NP6qXoN39nIqM4=";
            var salt = "zzWITpnDximUNKYLiUam/w==";
            var password = "secret1";

            this.Given(x => GivenThePassword(password))
                .And(x => GivenTheHash(hash))
                .And(x => GivenTheSalt(salt))
                .When(x => WhenIMatch())
                .Then(x => ThenTheResultIs(false))
                .BDDfy();
        }

        private void GivenThePassword(string password)
        {
            _password = password;
        }

        private void GivenTheHash(string hash)
        {
            _hash = hash;
        }

        private void GivenTheSalt(string salt)
        {
            _salt = salt;
        }

        private void WhenIMatch()
        {
            _result = _hashMatcher.Match(_password, _salt, _hash);
        }

        private void ThenTheResultIs(bool expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
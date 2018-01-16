using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class HeaderFindAndReplaceCreatorTests
    {
        private HeaderFindAndReplaceCreator _creator;
        private FileReRoute _reRoute;
        private List<HeaderFindAndReplace> _result;

        public HeaderFindAndReplaceCreatorTests()
        {
            _creator = new HeaderFindAndReplaceCreator();
        }

        [Fact]
        public void should_create()
        {
            var reRoute = new FileReRoute
            {
                UpstreamHeaderTransform = new Dictionary<string, string>
                {
                    {"Test", "Test, Chicken"},

                    {"Moop", "o, a"}
                }
            };

            var expected = new List<HeaderFindAndReplace>
            {
                new HeaderFindAndReplace("Test", "Test", "Chicken", 0),
                new HeaderFindAndReplace("Moop", "o", "a", 0)
            };

            this.Given(x => GivenTheReRoute(reRoute))
                .When(x => WhenICreate())
                .Then(x => ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheReRoute(FileReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_reRoute);
        }

        private void ThenTheFollowingIsReturned(List<HeaderFindAndReplace> expecteds)
        {
            _result.Count.ShouldBe(expecteds.Count);
            
            for (int i = 0; i < _result.Count; i++)
            {
                var result = _result[i];
                var expected = expecteds[i];
                result.Find.ShouldBe(expected.Find);
                result.Index.ShouldBe(expected.Index);
                result.Key.ShouldBe(expected.Key);
                result.Replace.ShouldBe(expected.Replace);
            }
        }
    }
}
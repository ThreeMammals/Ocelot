using System;
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
        private HeaderTransformations _result;

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
                },
                 DownstreamHeaderTransform = new Dictionary<string, string>
                {
                    {"Pop", "West, East"},

                    {"Bop", "e, r"}
                }
            };

            var upstream = new List<HeaderFindAndReplace>
            {
                new HeaderFindAndReplace("Test", "Test", "Chicken", 0),
                new HeaderFindAndReplace("Moop", "o", "a", 0)
            };

            var downstream = new List<HeaderFindAndReplace>
            {
                new HeaderFindAndReplace("Pop", "West", "East", 0),
                new HeaderFindAndReplace("Bop", "e", "r", 0)
            };

            this.Given(x => GivenTheReRoute(reRoute))
                .When(x => WhenICreate())
                .Then(x => ThenTheFollowingUpstreamIsReturned(upstream))
                .Then(x => ThenTheFollowingDownstreamIsReturned(downstream))
                .BDDfy();
        }

        private void ThenTheFollowingDownstreamIsReturned(List<HeaderFindAndReplace> downstream)
        {
            _result.Downstream.Count.ShouldBe(downstream.Count);
            
            for (int i = 0; i < _result.Downstream.Count; i++)
            {
                var result = _result.Downstream[i];
                var expected = downstream[i];
                result.Find.ShouldBe(expected.Find);
                result.Index.ShouldBe(expected.Index);
                result.Key.ShouldBe(expected.Key);
                result.Replace.ShouldBe(expected.Replace);
            }        
        }

        private void GivenTheReRoute(FileReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_reRoute);
        }

        private void ThenTheFollowingUpstreamIsReturned(List<HeaderFindAndReplace> expecteds)
        {
            _result.Upstream.Count.ShouldBe(expecteds.Count);
            
            for (int i = 0; i < _result.Upstream.Count; i++)
            {
                var result = _result.Upstream[i];
                var expected = expecteds[i];
                result.Find.ShouldBe(expected.Find);
                result.Index.ShouldBe(expected.Index);
                result.Key.ShouldBe(expected.Key);
                result.Replace.ShouldBe(expected.Replace);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
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
        private Mock<IBaseUrlFinder> _finder;

        public HeaderFindAndReplaceCreatorTests()
        {
            _finder = new Mock<IBaseUrlFinder>();
            _creator = new HeaderFindAndReplaceCreator(_finder.Object);
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

        [Fact]
        public void should_use_base_url_placeholder()
        {
            var reRoute = new FileReRoute
            {
                 DownstreamHeaderTransform = new Dictionary<string, string>
                {
                    {"Location", "http://www.bbc.co.uk/, {BaseUrl}"},
                }
            };

            var downstream = new List<HeaderFindAndReplace>
            {
                new HeaderFindAndReplace("Location", "http://www.bbc.co.uk/", "http://ocelot.com/", 0),
            };

            this.Given(x => GivenTheReRoute(reRoute))
                .And(x => GivenTheBaseUrlIs("http://ocelot.com/"))
                .When(x => WhenICreate())
                .Then(x => ThenTheFollowingDownstreamIsReturned(downstream))
                .BDDfy();
        }


        [Fact]
        public void should_use_base_url_partial_placeholder()
        {
            var reRoute = new FileReRoute
            {
                 DownstreamHeaderTransform = new Dictionary<string, string>
                {
                    {"Location", "http://www.bbc.co.uk/pay, {BaseUrl}pay"},
                }
            };

            var downstream = new List<HeaderFindAndReplace>
            {
                new HeaderFindAndReplace("Location", "http://www.bbc.co.uk/pay", "http://ocelot.com/pay", 0),
            };

            this.Given(x => GivenTheReRoute(reRoute))
                .And(x => GivenTheBaseUrlIs("http://ocelot.com/"))
                .When(x => WhenICreate())
                .Then(x => ThenTheFollowingDownstreamIsReturned(downstream))
                .BDDfy();
        }

        private void GivenTheBaseUrlIs(string baseUrl)
        {
            _finder.Setup(x => x.Find()).Returns(baseUrl);
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
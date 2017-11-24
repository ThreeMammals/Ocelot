using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class UpstreamTemplatePatternCreatorTests
    {
        private FileReRoute _fileReRoute;
        private UpstreamTemplatePatternCreator _creator;
        private string _result;

        public UpstreamTemplatePatternCreatorTests()
        {
            _creator = new UpstreamTemplatePatternCreator();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_ignore_case_sensitivity()
        {
            var fileReRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/PRODUCTS/{productId}",
                ReRouteIsCaseSensitive = false
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^(?i)/PRODUCTS/[0-9a-zA-Z].*$"))
                .BDDfy();
        }


        [Fact]
        public void should_match_forward_slash_or_no_forward_slash_if_template_end_with_forward_slash()
        {
            var fileReRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/PRODUCTS/",
                ReRouteIsCaseSensitive = false
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^(?i)/PRODUCTS(/|)$"))
                .BDDfy();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_respect_case_sensitivity()
        {
                var fileReRoute = new FileReRoute
                {
                    UpstreamPathTemplate = "/PRODUCTS/{productId}",
                    ReRouteIsCaseSensitive = true
                };
            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^/PRODUCTS/[0-9a-zA-Z].*$"))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_anything_to_end_of_string()
        {
            var fileReRoute =  new FileReRoute
            {
                UpstreamPathTemplate = "/api/products/{productId}",
                ReRouteIsCaseSensitive = true
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^/api/products/[0-9a-zA-Z].*$"))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder()
        {
            var fileReRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}",
                ReRouteIsCaseSensitive = true
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^/api/products/[0-9a-zA-Z].*/variants/[0-9a-zA-Z].*$"))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder_with_trailing_slash()
        {
            var fileReRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}/",
                ReRouteIsCaseSensitive = true
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^/api/products/[0-9a-zA-Z].*/variants/[0-9a-zA-Z].*(/|)$"))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_to_end_of_string()
        {
            var fileReRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/"
            };

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .When(x => x.WhenICreateTheTemplatePattern())
                .Then(x => x.ThenTheFollowingIsReturned("^/$"))
                .BDDfy();
        }

        private void GivenTheFollowingFileReRoute(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void WhenICreateTheTemplatePattern()
        {
            _result = _creator.Create(_fileReRoute);
        }

        private void ThenTheFollowingIsReturned(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
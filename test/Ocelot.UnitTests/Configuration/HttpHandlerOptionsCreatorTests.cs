﻿using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class HttpHandlerOptionsCreatorTests
    {
        private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private FileReRoute _fileReRoute;
        private HttpHandlerOptions _httpHandlerOptions;

        public HttpHandlerOptionsCreatorTests()
        {
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator();
        }

        [Fact]
        public void should_create_options_with_useCookie_false_and_allowAutoRedirect_true_as_default()
        {
            var fileReRoute = new FileReRoute();
            var expectedOptions = new HttpHandlerOptions(false, false, false);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_options_with_specified_useCookie_and_allowAutoRedirect()
        {
            var fileReRoute = new FileReRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    AllowAutoRedirect = false,
                    UseCookieContainer = false,
                    UseTracing = false
                }
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        private void GivenTheFollowing(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void WhenICreateHttpHandlerOptions()
        {
            _httpHandlerOptions = _httpHandlerOptionsCreator.Create(_fileReRoute);
        }

        private void ThenTheFollowingOptionsReturned(HttpHandlerOptions expected)
        {
            _httpHandlerOptions.ShouldNotBeNull();
            _httpHandlerOptions.AllowAutoRedirect.ShouldBe(expected.AllowAutoRedirect);
            _httpHandlerOptions.UseCookieContainer.ShouldBe(expected.UseCookieContainer);
            _httpHandlerOptions.UseTracing.ShouldBe(expected.UseTracing);
        }
    }
}

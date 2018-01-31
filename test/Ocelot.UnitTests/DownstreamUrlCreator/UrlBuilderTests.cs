using System;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    public class UrlBuilderTests
    {
        private readonly IUrlBuilder _urlBuilder;
        private string _dsPath;
        private string _dsScheme;
        private string _dsHost;
        private int _dsPort;

        private Response<DownstreamUrl> _result;

        public UrlBuilderTests()
        {
            _urlBuilder = new UrlBuilder();
        }

        [Fact]
        public void should_return_error_when_downstream_path_is_null()
        {
            this.Given(x => x.GivenADownstreamPath(null))
                .When(x => x.WhenIBuildTheUrl())
                .Then(x => x.ThenThereIsAnErrorOfType<DownstreamPathNullOrEmptyError>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_downstream_scheme_is_null()
        {
            this.Given(x => x.GivenADownstreamScheme(null))
                .And(x => x.GivenADownstreamPath("test"))
                .When(x => x.WhenIBuildTheUrl())
                .Then(x => x.ThenThereIsAnErrorOfType<DownstreamSchemeNullOrEmptyError>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_downstream_host_is_null()
        {
            this.Given(x => x.GivenADownstreamScheme(null))
                .And(x => x.GivenADownstreamPath("test"))
                .And(x => x.GivenADownstreamScheme("test"))
                .When(x => x.WhenIBuildTheUrl())
                .Then(x => x.ThenThereIsAnErrorOfType<DownstreamHostNullOrEmptyError>())
                .BDDfy();
        }

        [Fact]
        public void should_not_use_port_if_zero()
        {
            this.Given(x => x.GivenADownstreamPath("/api/products/1"))
           .And(x => x.GivenADownstreamScheme("http"))
           .And(x => x.GivenADownstreamHost("127.0.0.1"))
           .And(x => x.GivenADownstreamPort(0))
           .When(x => x.WhenIBuildTheUrl())
           .Then(x => x.ThenTheUrlIsReturned("http://127.0.0.1/api/products/1"))
           .And(x => x.ThenTheUrlIsWellFormed())
           .BDDfy();
        }

        [Fact]
        public void should_build_well_formed_uri()
        {
            this.Given(x => x.GivenADownstreamPath("/api/products/1"))
                .And(x => x.GivenADownstreamScheme("http"))
                .And(x => x.GivenADownstreamHost("127.0.0.1"))
                .And(x => x.GivenADownstreamPort(5000))
                .When(x => x.WhenIBuildTheUrl())
                .Then(x => x.ThenTheUrlIsReturned("http://127.0.0.1:5000/api/products/1"))
                .And(x => x.ThenTheUrlIsWellFormed())
                .BDDfy();
        }

        private void ThenThereIsAnErrorOfType<T>()
        {
            _result.Errors[0].ShouldBeOfType<T>();
        }

        private void GivenADownstreamPath(string dsPath)
        {
            _dsPath = dsPath;
        }

        private void GivenADownstreamScheme(string dsScheme)
        {
            _dsScheme = dsScheme;
        }

        private void GivenADownstreamHost(string dsHost)
        {
            _dsHost = dsHost;
        }

        private void GivenADownstreamPort(int dsPort)
        {
            _dsPort = dsPort;
        }

        private void WhenIBuildTheUrl()
        {
            _result = _urlBuilder.Build(_dsPath, _dsScheme, new ServiceHostAndPort(_dsHost, _dsPort));
        }

        private void ThenTheUrlIsReturned(string expected)
        {
            _result.Data.Value.ShouldBe(expected);
        }

        private void ThenTheUrlIsWellFormed()
        {
            Uri.IsWellFormedUriString(_result.Data.Value, UriKind.Absolute).ShouldBeTrue();
        }
    }
}

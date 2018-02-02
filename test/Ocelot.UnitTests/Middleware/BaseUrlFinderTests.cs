using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Ocelot.Middleware;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class BaseUrlFinderTests
    {
        private readonly BaseUrlFinder _baseUrlFinder;
        private readonly Mock<IWebHostBuilder> _webHostBuilder;
        private string _result;

        public BaseUrlFinderTests()
        {
            _webHostBuilder = new Mock<IWebHostBuilder>();
            _baseUrlFinder = new BaseUrlFinder(_webHostBuilder.Object);
        }

        [Fact]
        public void should_find_base_url_based_on_webhostbuilder()
        {
            this.Given(x => GivenTheWebHostBuilderReturns("http://localhost:7000"))
                .When(x => WhenIFindTheUrl())
                .Then(x => ThenTheUrlIs("http://localhost:7000"))
                .BDDfy();
        }

        [Fact]
        public void should_use_default_base_url()
        {
            this.Given(x => GivenTheWebHostBuilderReturns(""))
              .When(x => WhenIFindTheUrl())
              .Then(x => ThenTheUrlIs("http://localhost:5000"))
              .BDDfy();
        }

        private void GivenTheWebHostBuilderReturns(string url)
        {
            _webHostBuilder
                .Setup(x => x.GetSetting(WebHostDefaults.ServerUrlsKey))
                .Returns(url);
        }

        private void WhenIFindTheUrl()
        {
            _result = _baseUrlFinder.Find();
        }

        private void ThenTheUrlIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}

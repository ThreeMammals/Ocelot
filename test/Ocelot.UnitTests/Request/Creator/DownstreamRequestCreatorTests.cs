using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Ocelot.Infrastructure;
using Ocelot.Request.Creator;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Request.Creator
{
    public class DownstreamRequestCreatorTests
    {
        [Fact]
        public async Task should_create_downstream_request()
        {
            var framework = new Mock<IFrameworkDescription>();
            framework.Setup(x => x.Get()).Returns("");
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
            var content = new StringContent("test");
            request.Content = content;
            var downstreamRequestCreator = new DownstreamRequestCreator(framework.Object);
            var result = downstreamRequestCreator.Create(request);
            result.ShouldNotBeNull();
            result.Method.ToLower().ShouldBe("get");
            result.Scheme.ToLower().ShouldBe("http");
            result.Host.ToLower().ShouldBe("www.test.com");
            var resultContent = await result.ToHttpRequestMessage().Content.ReadAsStringAsync();
            resultContent.ShouldBe("test");
        }

        [Fact]
        public void should_remove_body()
        {
            var framework = new Mock<IFrameworkDescription>();
            framework.Setup(x => x.Get()).Returns(".NET Framework");
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
            var content = new StringContent("test");
            request.Content = content;
            var downstreamRequestCreator = new DownstreamRequestCreator(framework.Object);
            var result = downstreamRequestCreator.Create(request);
            result.ShouldNotBeNull();
            result.Method.ToLower().ShouldBe("get");
            result.Scheme.ToLower().ShouldBe("http");
            result.Host.ToLower().ShouldBe("www.test.com");
            result.ToHttpRequestMessage().Content.ShouldBeNull();
        }
    }
}

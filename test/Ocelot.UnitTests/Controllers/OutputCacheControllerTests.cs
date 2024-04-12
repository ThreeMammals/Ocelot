using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;

namespace Ocelot.UnitTests.Controllers
{
    public class OutputCacheControllerTests : UnitTest
    {
        private readonly OutputCacheController _controller;
        private readonly Mock<IOcelotCache<CachedResponse>> _cache;
        private IActionResult _result;

        public OutputCacheControllerTests()
        {
            _cache = new Mock<IOcelotCache<CachedResponse>>();
            _controller = new OutputCacheController(_cache.Object);
        }

        [Fact]
        public void should_delete_key()
        {
            this.When(_ => WhenIDeleteTheKey("a"))
               .Then(_ => ThenTheKeyIsDeleted("a"))
               .BDDfy();
        }

        private void ThenTheKeyIsDeleted(string key)
        {
            _result.ShouldBeOfType<NoContentResult>();
            _cache
                .Verify(x => x.ClearRegion(key), Times.Once);
        }

        private void WhenIDeleteTheKey(string key)
        {
            _result = _controller.Delete(key);
        }
    }
}

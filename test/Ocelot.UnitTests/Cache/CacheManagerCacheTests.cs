using System;
using CacheManager.Core;
using Moq;
using Ocelot.Cache;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cache
{
    public class CacheManagerCacheTests
    {
        private OcelotCacheManagerCache<string> _ocelotOcelotCacheManager;
        private Mock<ICacheManager<string>> _mockCacheManager;
        private string _key;
        private string _value;
        private string _resultGet;
        private TimeSpan _ttlSeconds;

        public CacheManagerCacheTests()
        {
            _mockCacheManager = new Mock<ICacheManager<string>>();
            _ocelotOcelotCacheManager = new OcelotCacheManagerCache<string>(_mockCacheManager.Object);
        }
        [Fact]
        public void should_get_from_cache()
        {
            this.Given(x => x.GivenTheFollowingIsCached("someKey", "someValue"))
                .When(x => x.WhenIGetFromTheCache())
                .Then(x => x.ThenTheResultIs("someValue"))
                .BDDfy();

        }

        [Fact]
        public void should_add_to_cache()
        {
            this.When(x => x.WhenIAddToTheCache("someKey", "someValue", TimeSpan.FromSeconds(1)))
                .Then(x => x.ThenTheCacheIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenIAddToTheCache(string key, string value, TimeSpan ttlSeconds)
        {
            _key = key;
            _value = value;
            _ttlSeconds = ttlSeconds;

            _ocelotOcelotCacheManager.Add(_key, _value, _ttlSeconds);
        }

        private void ThenTheCacheIsCalledCorrectly()
        {
            _mockCacheManager
                .Verify(x => x.Add(It.IsAny<CacheItem<string>>()), Times.Once);
        }

        private void ThenTheResultIs(string expected)
        {
            _resultGet.ShouldBe(expected);
        }

        private void WhenIGetFromTheCache()
        {
            _resultGet = _ocelotOcelotCacheManager.Get(_key);
        }

        private void GivenTheFollowingIsCached(string key, string value)
        {
            _key = key;
            _value = value;
            _mockCacheManager
                .Setup(x => x.Get<string>(It.IsAny<string>()))
                .Returns(value);
        }
    }
}

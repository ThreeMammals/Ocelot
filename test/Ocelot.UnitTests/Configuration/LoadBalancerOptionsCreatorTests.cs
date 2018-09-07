using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class LoadBalancerOptionsCreatorTests
    {
        private ILoadBalancerOptionsCreator _creator;

        public LoadBalancerOptionsCreatorTests()
        {
            _creator = new LoadBalancerOptionsCreator();
        }

        [Fact]
        public void should_do_a_thing()
        {
            var fileLoadBalancerOptions = new FileLoadBalancerOptions
            {
                Type = "test",
                Key = "west",
                Expiry = 1
            };

            var result = _creator.CreateLoadBalancerOptions(fileLoadBalancerOptions);

            result.Type.ShouldBe(fileLoadBalancerOptions.Type);
            result.Key.ShouldBe(fileLoadBalancerOptions.Key);
            result.ExpiryInMs.ShouldBe(fileLoadBalancerOptions.Expiry);
        }
    }
}

using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Infrastructure
{
    public class InMemoryBusTests
    {
        private InMemoryBus<Message> _bus;

        public InMemoryBusTests()
        {
            _bus = new InMemoryBus<Message>();
        }

        [Fact]
        public async Task should_publish_with_delay()
        {
            var called = false;
            _bus.Subscribe(x => {
                called = true;
            });
            await _bus.Publish(new Message(), 1);
            await Task.Delay(10);
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task should_not_be_publish_yet_as_no_delay_in_caller()
        {
            var called = false;
            _bus.Subscribe(x => {
                called = true;
            });
            await _bus.Publish(new Message(), 1);
            called.ShouldBeFalse();
        }


        class Message
        {
            
        }
    }
}

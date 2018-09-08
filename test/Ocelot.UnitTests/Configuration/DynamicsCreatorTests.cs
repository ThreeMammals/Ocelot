namespace Ocelot.UnitTests.Configuration
{
    using Moq;
    using Ocelot.Configuration.Creator;
    using Xunit;

    public class DynamicsCreatorTests
    {
        private DynamicsCreator _creator;
        private Mock<IRateLimitOptionsCreator> _rloCreator;

        public DynamicsCreatorTests()
        {
            _rloCreator = new Mock<IRateLimitOptionsCreator>();
            _creator = new DynamicsCreator(_rloCreator.Object);
        }

        [Fact]
        public void should_do()
        {
            
        }
    }
}

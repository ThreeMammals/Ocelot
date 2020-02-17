namespace Ocelot.UnitTests.Configuration
{
    using Ocelot.Configuration.Creator;
    using Shouldly;
    using Xunit;

    public class VersionCreatorTests
    {
        private readonly VersionCreator _creator;

        public VersionCreatorTests()
        {
            _creator = new VersionCreator();
        }

        [Fact]
        public void should_create_version_based_on_input()
        {
            var input = "2.0";
            var result = _creator.Create(input);
            result.Major.ShouldBe(2);
            result.Minor.ShouldBe(0);
        }

        [Fact]
        public void should_default_to_version_one_point_one()
        {
            var input = "";
            var result = _creator.Create(input);
            result.Major.ShouldBe(1);
            result.Minor.ShouldBe(1);
        }
    }
}

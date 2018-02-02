using Ocelot.Configuration.Creator;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class IdentityServerConfigurationCreatorTests
    {
        [Fact]
        public void happy_path_only_exists_for_test_coverage_even_uncle_bob_probably_wouldnt_test_this()
        {
            var result = IdentityServerConfigurationCreator.GetIdentityServerConfiguration("secret");
            result.ApiName.ShouldBe("admin");
        }
    }
}
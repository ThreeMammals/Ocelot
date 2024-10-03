using Ocelot.Infrastructure;
using System.Text.Json;

namespace Ocelot.UnitTests
{
    public class JsonSerializerOptionsFactoryTests : UnitTest
    {
        [Fact]
        public void should_json_path()
        {
            //Arrange
            var json =
                "[{\"id\":1,\"writerId\":1,\"postId\":2,\"text\":\"text1\"},{\"id\":2,\"writerId\":1,\"postId\":2,\"text\":\"text2\"}]";
            var path = "$[*].writerId";
            var document = JsonDocument.Parse(json);

            //Act
            var result = document.ExtractValuesFromJsonPath(path);

            //Assert
            result.ShouldBeEquivalentTo(new List<string> { "1", "1" });
        }

        [Fact]
        public void should_json_path_nested()
        {
            //Arrange
            var json =
                "[{\"id\":1,\"writerId\":1,\"postId\":2,\"text\":\"text1\",\"comments\":[{\"commentId\":1,\"text\":\"Good post!\"},{\"commentId\":2,\"text\":\"Nice post!\"}]},{\"id\":2,\"writerId\":2,\"postId\":2,\"text\":\"text2\",\"comments\":[{\"commentId\":3,\"text\":\"Interesting.\"}]}]";
            var path = "$[*].comments[*].text";
            var document = JsonDocument.Parse(json);

            //Act
            var result = document.ExtractValuesFromJsonPath(path);

            //Assert
            result.ShouldBeEquivalentTo(new List<string> { "Good post!", "Nice post!", "Interesting." });
        }

        [Fact]
        public void should_json_path_nested_null()
        {
            //Arrange
            var json =
                "[{\"id\":1,\"writerId\":1,\"postId\":2,\"text\":\"text1\",\"comments\":[{\"commentId\":1,\"text\":null},{\"commentId\":2,\"text\":\"Nice post!\"}]},{\"id\":2,\"writerId\":2,\"postId\":2,\"text\":\"text2\",\"comments\":[{\"commentId\":3,\"text\":\"Interesting.\"}]}]";
            var path = "$[*].comments[*].text";
            var document = JsonDocument.Parse(json);

            //Act
            var result = document.ExtractValuesFromJsonPath(path);

            //Assert
            result.ShouldBeEquivalentTo(new List<string> { "Nice post!", "Interesting." });
        }
    }
}

using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Infrastructure;

public class HttpDataRepositoryTests : UnitTest
{
    private readonly Mock<IHttpContextAccessor> _contextAccessor;
    private readonly HttpDataRepository _repository;
    private readonly DefaultHttpContext _httpContext;

    public HttpDataRepositoryTests()
    {
        _contextAccessor = new();
        _repository = new(_contextAccessor.Object);
        _httpContext = new()
        {
            Items = new Dictionary<object, object>()
        };
        _contextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    [Fact]
    public void Constructor_Should_Throw_If_ContextAccessor_Is_Null()
    {
        // Arrange
        IHttpContextAccessor contextAccessor = null;

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => new HttpDataRepository(contextAccessor));

        // Assert
        Assert.Equal(nameof(contextAccessor), ex.ParamName);
    }

    [Fact]
    public void Add_Should_Return_OkResponse_When_Successful()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        var response = _repository.Add(key, value);

        // Assert
        Assert.IsType<OkResponse>(response);
        Assert.False(response.IsError);
        Assert.Empty(response.Errors); // OkResponse has no errors
        Assert.True(_httpContext.Items.ContainsKey(key));
        Assert.Equal(value, _httpContext.Items[key]);
    }

    [Fact]
    public void Add_Should_Return_ErrorResponse_When_Exception_Occurs()
    {
        var coll = new Dictionary<object, object>();
        _httpContext.Items = coll;
        coll.Add("key", "value");

        // Act
        var response = _repository.Add("key", "value");

        // Assert
        Assert.IsType<ErrorResponse>(response);
        Assert.True(response.IsError);
        var error = Assert.Single(response.Errors);
        Assert.IsType<CannotAddDataError>(error);
        Assert.Equal("An item with the same key has already been added. Key: key", error.Message);
        Assert.IsType<ArgumentException>(error.Exception);
    }

    [Fact]
    public void Update_Should_Return_OkResponse_And_Update_Value()
    {
        // Arrange
        const string key = "update-key";
        _httpContext.Items[key] = "old-value";

        // Act
        var response = _repository.Update(key, "new-value");

        // Assert
        Assert.IsType<OkResponse>(response);
        Assert.Equal("new-value", _httpContext.Items[key]);
    }

    [Fact]
    public void Update_Should_Return_ErrorResponse_When_HttpContext_Is_Null()
    {
        // Arrange
        _contextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        // Act
        var response = _repository.Update("any-key", "value");

        // Assert
        Assert.IsType<ErrorResponse>(response);
        Assert.True(response.IsError);
        var error = Assert.Single(response.Errors);
        Assert.IsType<CannotAddDataError>(error);
        Assert.Equal("Object reference not set to an instance of an object.", error.Message);
        Assert.NotNull(error.Exception);
    }

    [Fact]
    public void Get_Should_Return_OkResponse_With_Value_When_Key_Exists()
    {
        // Arrange
        const string key = "existing-key";
        const int value = 42;
        _httpContext.Items[key] = value;

        // Act
        var response = _repository.Get<int>(key);

        // Assert
        Assert.IsType<OkResponse<int>>(response);
        Assert.Equal(value, response.Data);
        Assert.False(response.IsError);
    }

    [Fact]
    public void Get_Should_Return_ErrorResponse_When_Key_Does_Not_Exist()
    {
        // Arrange
        const string key = "non-existent-key";

        // Act
        var response = _repository.Get<string>(key);

        // Assert
        Assert.IsType<ErrorResponse<string>>(response);
        Assert.True(response.IsError);
        var error = Assert.Single(response.Errors);
        Assert.IsType<CannotFindDataError>(error);
        Assert.Equal($"Unable to find data for key: {key}", error.Message);
    }

    [Fact]
    public void Get_Should_Return_ErrorResponse_When_HttpContext_Is_Null()
    {
        // Arrange
        _contextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        // Act
        var response = _repository.Get<string>("any-key");

        // Assert
        Assert.IsType<ErrorResponse<string>>(response);
        Assert.True(response.IsError);
        var error = Assert.Single(response.Errors);
        Assert.IsType<CannotFindDataError>(error);
        Assert.Contains("because HttpContext or HttpContext.Items is null", error.Message);
    }

    [Fact]
    public void Get_Should_Return_ErrorResponse_When_Items_Is_Null()
    {
        // Arrange
        var context = new DefaultHttpContext { Items = null! };
        _contextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var response = _repository.Get<object>("key");

        // Assert
        Assert.IsType<ErrorResponse<object>>(response);
        var error = Assert.Single(response.Errors);
        Assert.IsType<CannotFindDataError>(error);
    }

    [Fact]
    public void Get_Should_Handle_Type_Casting_Correctly()
    {
        // Arrange
        const string key = "typed-key";
        _httpContext.Items[key] = 123;

        // Act
        var response = _repository.Get<int>(key);

        // Assert
        Assert.False(response.IsError);
        Assert.Equal(123, response.Data);
    }

    [Fact]
    public void Add_And_Get_Should_Work_Together()
    {
        // Arrange
        const string key = "combined-test";
        var complexValue = new { Name = "Test", Value = 100 };

        // Act
        _repository.Add(key, complexValue);
        var result = _repository.Get<object>(key);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Data);
    }
}

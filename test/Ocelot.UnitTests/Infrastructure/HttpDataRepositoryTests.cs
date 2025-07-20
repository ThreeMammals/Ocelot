using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Infrastructure;

public class HttpDataRepositoryTests : UnitTest
{
    private readonly DefaultHttpContext _httpContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpDataRepository _repository;
    private object _result;

    public HttpDataRepositoryTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContextAccessor = new HttpContextAccessor { HttpContext = _httpContext };
        _repository = new HttpDataRepository(_httpContextAccessor);
    }

    /*
    TODO - Additional tests -> Type mistmatch aka Add string, request int
    TODO - Additional tests -> HttpContent null. This should never happen
    */
    [Fact]
    public void Get_returns_correct_key_from_http_context()
    {
        // Arrange
        _httpContext.Items.Add("key", "string");

        // Act
        _result = _repository.Get<string>("key");

        // Assert
        ThenTheResultIsAnOkResponse<string>("string");
    }

    [Fact]
    public void Get_returns_error_response_if_the_key_is_not_found() //Therefore does not return null
    {
        // Arrange
        _httpContext.Items.Add("key", "string");

        // Act
        _result = _repository.Get<string>("keyDoesNotExist");

        // Assert
        ThenTheResultIsAnErrorReposnse<string>();
    }

    [Fact]
    public void Should_update()
    {
        // Arrange
        _httpContext.Items.Add("key", "string");
        _repository.Update<string>("key", "new string");

        // Act
        _result = _repository.Get<string>("key");

        // Assert
        ThenTheResultIsAnOkResponse<string>("new string");
    }

    private void ThenTheResultIsAnErrorReposnse<T>()
    {
        _result.ShouldBeOfType<ErrorResponse<T>>();
        ((ErrorResponse<T>)_result).Data.ShouldBe(default);
        ((ErrorResponse<T>)_result).IsError.ShouldBe(true);
        ((ErrorResponse<T>)_result).Errors.ShouldHaveSingleItem()
            .ShouldBeOfType<CannotFindDataError>()
            .Message.ShouldStartWith("Unable to find data for key: ");
    }

    private void ThenTheResultIsAnOkResponse<T>(object resultValue)
    {
        _result.ShouldBeOfType<OkResponse<T>>();
        ((OkResponse<T>)_result).Data.ShouldBe(resultValue);
    }
}

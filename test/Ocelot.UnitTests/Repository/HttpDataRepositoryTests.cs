using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.UnitTests.Repository;

public class HttpDataRepositoryTests : UnitTest
{
    private readonly HttpDataRepository _repository;
    private readonly HttpContextAccessor _contextAccesor;

    public HttpDataRepositoryTests()
    {
        _contextAccesor = new()
        {
            HttpContext = new DefaultHttpContext(),
        };
        _repository = new HttpDataRepository(_contextAccesor);
    }

    [Fact]
    public void Should_add_item()
    {
        // Arrange
        const string key = "blahh";
        var toAdd = new[] { 1, 2, 3, 4 };

        // Act
        _repository.Add(key, toAdd);

        // Assert
        _contextAccesor.HttpContext.Items.TryGetValue(key, out var obj).ShouldBeTrue();
        obj.ShouldNotBeNull();
        var arr = (int[])obj;
        arr.ShouldNotBeNull();
        arr.ShouldContain(4);
    }

    [Fact]
    public void Should_get_item()
    {
        // Arrange
        const string key = "chest";
        var data = new[] { 5435345 };
        _contextAccesor.HttpContext.Items.Add(key, data);

        // Act
        var result = _repository.Get<int[]>(key);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Data.ShouldNotBeNull();
    }
}

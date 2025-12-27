using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Request.Mapper;
using Ocelot.Requester;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Requester;

[Trait("Feat", "562")] // https://github.com/ThreeMammals/Ocelot/pull/562
[Trait("Release", "10.0.3")] // https://github.com/ThreeMammals/Ocelot/releases/tag/10.0.3
public class HttpExceptionToErrorMapperTests
{
    private HttpExceptionToErrorMapper _mapper;
    private readonly ServiceCollection _services;

    public HttpExceptionToErrorMapperTests()
    {
        _services = new ServiceCollection();
        var provider = _services.BuildServiceProvider(true);
        _mapper = new HttpExceptionToErrorMapper(provider);
    }

    [Fact]
    public void Should_return_default_error_because_mappers_are_null()
    {
        // Arrange, Act
        var error = _mapper.Map(new Exception());

        // Assert
        error.ShouldBeOfType<UnableToCompleteRequestError>();
    }

    [Fact]
    [Trait("PR", "902")] // https://github.com/ThreeMammals/Ocelot/pull/902
    public void Should_return_request_canceled()
    {
        // Arrange, Act
        var error = _mapper.Map(new OperationCanceledException());

        // Assert
        error.ShouldBeOfType<RequestCanceledError>();
    }

    [Fact]
    public void Should_return_ConnectionToDownstreamServiceError()
    {
        // Arrange, Act
        var error = _mapper.Map(new HttpRequestException());

        // Assert
        error.ShouldBeOfType<ConnectionToDownstreamServiceError>();
    }

    private class SomeException : OperationCanceledException { }

    [Fact]
    public void Should_return_request_canceled_for_subtype()
    {
        // Arrange, Act
        var error = _mapper.Map(new SomeException());

        // Assert
        error.ShouldBeOfType<RequestCanceledError>();
    }

    [Fact]
    public void Should_return_error_from_mapper()
    {
        // Arrange
        IDictionary<Type, Func<Exception, Error>> errorMapping = new Dictionary<Type, Func<Exception, Error>>
        {
            {typeof(TaskCanceledException), e => new AnyError()},
        };

        _services.AddSingleton(errorMapping);

        var provider = _services.BuildServiceProvider(true);

        _mapper = new HttpExceptionToErrorMapper(provider);

        // Act
        var error = _mapper.Map(new TaskCanceledException());

        // Assert
        error.ShouldBeOfType<AnyError>();
    }

    [Fact]
    [Trait("PR", "1824")] // https://github.com/ThreeMammals/Ocelot/pull/1824
    public void Map_TimeoutException_To_RequestTimedOutError()
    {
        // Arrange
        var ex = new TimeoutException("test");

        // Act
        var error = _mapper.Map(ex);

        // Assert
        Assert.IsType<RequestTimedOutError>(error);
        Assert.Equal(25, (int)error.Code);
        Assert.Equal(503, error.HttpStatusCode);
        Assert.Equal("Timeout making http request, exception: System.TimeoutException: test", error.Message);
    }

    [Fact]
    [Trait("Bug", "749")] // https://github.com/ThreeMammals/Ocelot/issues/749
    [Trait("PR", "1769")] // https://github.com/ThreeMammals/Ocelot/pull/1769
    public void Map_BadHttpRequestException_To_PayloadTooLargeError()
    {
        // Arrange
        var inner = new BadHttpRequestException("test-inner", 413);
        var ex = new HttpRequestException("test", inner);

        // Act
        var error = _mapper.Map(ex);

        // Assert
        Assert.IsType<PayloadTooLargeError>(error);
        Assert.Equal(41, (int)error.Code);
        Assert.Equal(413, error.HttpStatusCode);
        Assert.Equal("test", error.Message);
    }
}

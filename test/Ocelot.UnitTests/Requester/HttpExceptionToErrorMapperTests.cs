using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Requester;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Requester;

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

    private class SomeException : OperationCanceledException
    { }
}

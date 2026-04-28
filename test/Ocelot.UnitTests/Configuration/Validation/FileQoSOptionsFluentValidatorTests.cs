using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.Logging;
using Ocelot.QualityOfService;

namespace Ocelot.UnitTests.Configuration.Validation;

public class FileQoSOptionsFluentValidatorTests : UnitTest
{
    private FileQoSOptionsFluentValidator _validator;
    private readonly ServiceCollection _services;

    public FileQoSOptionsFluentValidatorTests()
    {
        _services = new ServiceCollection();
        var provider = _services.BuildServiceProvider(true);
        _validator = new FileQoSOptionsFluentValidator(provider);
    }

    [Fact]
    public void Should_be_valid_as_nothing_set()
    {
        // Arrange
        var qosOptions = new FileQoSOptions();

        // Act
        var result = _validator.Validate(qosOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_be_valid_as_qos_delegate_set()
    {
        // Arrange
        var qosOptions = new FileQoSOptions
        {
            Timeout = 1,
            MinimumThroughput = 1,
        };
        GivenAQosDelegate();

        // Act
        var result = _validator.Validate(qosOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_be_invalid_as_no_qos_delegate()
    {
        // Arrange
        var qosOptions = new FileQoSOptions
        {
            Timeout = 1,
            MinimumThroughput = 1,
        };

        // Act
        var result = _validator.Validate(qosOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Unable to start Ocelot because either a Route or GlobalConfiguration is using QoSOptions, but no QosDelegatingHandlerDelegate has been registered in the dependency injection container. Are you missing an external package like Ocelot.QualityOfService.Polly (and calling AddPolly()), or the built-in QoS support (via AddQualityOfService())?",
            result.Errors[0].ErrorMessage);
    }

    private void GivenAQosDelegate()
    {
        static DelegatingHandler Fake(DownstreamRoute a, IHttpContextAccessor b, IOcelotLoggerFactory c) => null;
        _services.AddSingleton((QosDelegatingHandlerDelegate)Fake);
        var provider = _services.BuildServiceProvider(true);
        _validator = new FileQoSOptionsFluentValidator(provider);
    }
}

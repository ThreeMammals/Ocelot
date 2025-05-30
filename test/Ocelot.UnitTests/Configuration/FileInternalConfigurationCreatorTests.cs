﻿using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Configuration;

public class FileInternalConfigurationCreatorTests : UnitTest
{
    private readonly Mock<IConfigurationValidator> _validator;
    private readonly Mock<IRoutesCreator> _routesCreator;
    private readonly Mock<IAggregatesCreator> _aggregatesCreator;
    private readonly Mock<IDynamicsCreator> _dynamicsCreator;
    private readonly Mock<IConfigurationCreator> _configCreator;
    private FileConfiguration _fileConfiguration;
    private readonly FileInternalConfigurationCreator _creator;
    private Response<IInternalConfiguration> _result;
    private List<Route> _routes;
    private List<Route> _aggregates;
    private List<Route> _dynamics;
    private InternalConfiguration _internalConfig;

    public FileInternalConfigurationCreatorTests()
    {
        _validator = new Mock<IConfigurationValidator>();
        _routesCreator = new Mock<IRoutesCreator>();
        _aggregatesCreator = new Mock<IAggregatesCreator>();
        _dynamicsCreator = new Mock<IDynamicsCreator>();
        _configCreator = new Mock<IConfigurationCreator>();

        _creator = new FileInternalConfigurationCreator(_validator.Object, _routesCreator.Object, _aggregatesCreator.Object, _dynamicsCreator.Object, _configCreator.Object);
    }

    [Fact]
    public async Task Should_return_validation_error()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheValidationFails();

        // Act
        _result = await _creator.Create(_fileConfiguration);

        // Assert
        _result.IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_return_internal_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheValidationSucceeds();
        GivenTheDependenciesAreSetUp();

        // Act
        _result = await _creator.Create(_fileConfiguration);

        // Assert
        ThenTheDependenciesAreCalledCorrectly();
    }

    private void ThenTheDependenciesAreCalledCorrectly()
    {
        _routesCreator.Verify(x => x.Create(_fileConfiguration), Times.Once);
        _aggregatesCreator.Verify(x => x.Create(_fileConfiguration, _routes), Times.Once);
        _dynamicsCreator.Verify(x => x.Create(_fileConfiguration), Times.Once);

        var mergedRoutes = _routes
            .Union(_aggregates)
            .Union(_dynamics)
            .ToList();

        _configCreator.Verify(x => x.Create(_fileConfiguration, It.Is<List<Route>>(y => y.Count == mergedRoutes.Count)), Times.Once);
    }

    private void GivenTheDependenciesAreSetUp()
    {
        _routes = new List<Route> { new RouteBuilder().Build() };
        _aggregates = new List<Route> { new RouteBuilder().Build() };
        _dynamics = new List<Route> { new RouteBuilder().Build() };
        _internalConfig = new InternalConfiguration(null, string.Empty, null, string.Empty, null, string.Empty, null, null, null, null);

        _routesCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).Returns(_routes);
        _aggregatesCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>(), It.IsAny<List<Route>>())).Returns(_aggregates);
        _dynamicsCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>())).Returns(_dynamics);
        _configCreator.Setup(x => x.Create(It.IsAny<FileConfiguration>(), It.IsAny<List<Route>>())).Returns(_internalConfig);
    }

    private void GivenTheValidationSucceeds()
    {
        var ok = new ConfigurationValidationResult(false);
        var response = new OkResponse<ConfigurationValidationResult>(ok);
        _validator.Setup(x => x.IsValid(It.IsAny<FileConfiguration>())).ReturnsAsync(response);
    }

    private void GivenTheValidationFails()
    {
        var error = new ConfigurationValidationResult(true, new List<Error> { new AnyError() });
        var response = new OkResponse<ConfigurationValidationResult>(error);
        _validator.Setup(x => x.IsValid(It.IsAny<FileConfiguration>())).ReturnsAsync(response);
    }
}

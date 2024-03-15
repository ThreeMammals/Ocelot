using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests;

[Trait("PR", "1227")]
[Trait("Issue", "1216")]
public sealed class ConfigurationMergeTests : Steps
{
    private readonly FileConfiguration _globalConfig;
    private readonly string _globalConfigFileName;

    public ConfigurationMergeTests() : base()
    {
        _globalConfig = new();
        _globalConfigFileName = $"{TestID}-{ConfigurationBuilderExtensions.GlobalConfigFile}";
    }

    protected override void DeleteOcelotConfig(params string[] files) => base.DeleteOcelotConfig(_globalConfigFileName);

    [Fact]
    public void Should_run_with_global_config_merged_to_memory()
    {
        Arrange();

        // Act
        GivenOcelotIsRunningMergedConfig(MergeOcelotJson.ToMemory);

        // Assert
        TheOcelotPrimaryConfigFileExists(false);
        Assert();
    }

    [Fact]
    public void Should_run_with_global_config_merged_to_file()
    {
        Arrange();

        // Act
        GivenOcelotIsRunningMergedConfig(MergeOcelotJson.ToFile);

        // Assert
        TheOcelotPrimaryConfigFileExists(true);
        Assert();
    }

    private void GivenOcelotIsRunningMergedConfig(MergeOcelotJson mergeTo)
        => StartOcelot((context, config) => config.AddOcelot(_globalConfig, context.HostingEnvironment, mergeTo, _ocelotConfigFileName, _globalConfigFileName, null, false, false));

    private void TheOcelotPrimaryConfigFileExists(bool expected)
        => File.Exists(_ocelotConfigFileName).ShouldBe(expected);

    private void Arrange([CallerMemberName] string testName = null)
    {
        _globalConfig.GlobalConfiguration.RequestIdKey = testName;
    }

    private void Assert([CallerMemberName] string testName = null)
    {
        var config = _ocelotServer.Services.GetService<IConfiguration>();
        config.ShouldNotBeNull();
        var actual = config["GlobalConfiguration:RequestIdKey"];
        actual.ShouldNotBeNull().ShouldBe(testName);
    }
}

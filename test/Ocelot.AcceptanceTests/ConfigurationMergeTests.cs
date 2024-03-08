using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests;

public sealed class ConfigurationMergeTests : Steps
{
    private readonly FileConfiguration _globalConfig;
    private readonly string _globalConfigFileName;

    public ConfigurationMergeTests() : base()
    {
        var testID = _ocelotConfigFileName.Split('-')[0];
        _globalConfigFileName = $"{testID}-{ConfigurationBuilderExtensions.GlobalConfigFile}";

        _globalConfig = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                RequestIdKey = "initialKey",
            },
        };
    }

    protected override void DeleteOcelotConfig(params string[] files)
    {
        base.DeleteOcelotConfig(_globalConfigFileName);
    }

    [Fact]
    public void Should_run_with_global_config_merged_to_memory()
    {
        this.Given(x => GivenThereIsAConfiguration(_globalConfig, _globalConfigFileName, false, false))
            .When(x => WhenOcelotIsRunningMergedConfig(MergeOcelotJson.ToMemory))
            .Then(x => TheOcelotPrimaryConfigFileExists(false))
            .BDDfy();
    }

    [Fact]
    public void Should_run_with_global_config_merged_to_file()
    {
        this.Given(x => GivenThereIsAConfiguration(_globalConfig))
            .When(x => WhenOcelotIsRunningMergedConfig(MergeOcelotJson.ToFile))
            .Then(x => TheOcelotPrimaryConfigFileExists(true))
            .BDDfy();
    }

    private void WhenOcelotIsRunningMergedConfig(MergeOcelotJson mergeTo)
        => StartOcelot((context, config) => config.AddOcelot(context.HostingEnvironment, mergeTo));

    private void TheOcelotPrimaryConfigFileExists(bool expected)
        => File.Exists(_ocelotConfigFileName).ShouldBe(expected);
}

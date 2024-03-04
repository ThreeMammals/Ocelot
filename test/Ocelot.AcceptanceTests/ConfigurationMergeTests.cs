using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests;

public sealed class ConfigurationMergeTests : IDisposable
{
    private readonly FileConfiguration _globalConfig;
    private readonly Steps _steps;

    public ConfigurationMergeTests()
    {
        _steps = new Steps();

        //if (File.Exists(TestConfiguration.PrimaryConfigurationPath))
        //{
        //    try
        //    {
        //        File.Delete(TestConfiguration.PrimaryConfigurationPath);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
        //}

        _globalConfig = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = "initialKey",
            },
        };
    }

    [Fact]
    public void Should_run_with_config_merged_to_memory()
    {
        this.Given(x => _steps.GivenThereIsAConfiguration(_globalConfig, TestConfiguration.ConfigurationPartPath("global")))
            .When(x => _steps.WhenOcelotIsRunningMergedConfig(MergeOcelotJson.ToMemory))
            .Then(x => _steps.TheOcelotJsonFileExists(false))
            .BDDfy();
    }

    [Fact]
    public void Should_run_with_config_merged_to_file()
    {
        this.Given(x => _steps.GivenThereIsAConfiguration(_globalConfig))
            .When(x => _steps.WhenOcelotIsRunningMergedConfig(MergeOcelotJson.ToFile))
            .Then(x => _steps.TheOcelotJsonFileExists(true))
            .BDDfy();
    }

    public void Dispose()
    {
        _steps.Dispose();
    }

    //private static void TheOcelotJsonFileExists(bool expected)
    //{
    //    File.Exists(_ocelotConfigFileName).ShouldBe(expected);
    //}
}

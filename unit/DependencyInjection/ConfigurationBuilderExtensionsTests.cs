using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.DependencyInjection;

public sealed class ConfigurationBuilderExtensionsTests : ConfigurationBuilderExtensionsTestsBase
{
    private IConfigurationRoot _configRoot;
    private readonly ITestOutputHelper _output;
    public ConfigurationBuilderExtensionsTests(ITestOutputHelper output)
        => _output = output;

    //protected override string EnvironmentName()
    //    => _hostingEnvironment?.Object?.EnvironmentName ?? base.EnvironmentName();

    [Fact]
    [Trait("Bug", "1216")] // https://github.com/ThreeMammals/Ocelot/issues/1216
    [Trait("PR", "1227")] // https://github.com/ThreeMammals/Ocelot/pull/1227
    public void Should_merge_files_to_file()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        GivenMultipleConfigurationFiles(TestID);

        // Act
        WhenIAddOcelotConfiguration(TestID);

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        TheOcelotPrimaryConfigFileExists(true);
    }

    [Fact]
    [Trait("Feat", "1228")] // https://github.com/ThreeMammals/Ocelot/issues/1228
    [Trait("Feat", "1235")] // https://github.com/ThreeMammals/Ocelot/issues/1235
    [Trait("Bug", "1247")] // https://github.com/ThreeMammals/Ocelot/issues/1247
    [Trait("PR", "1569")] // https://github.com/ThreeMammals/Ocelot/pull/1569
    [Trait("Commit", "4d245ec")] // https://github.com/ThreeMammals/Ocelot/commit/4d245ec7fa89b35189c8e2082238ddf1f590dcf8
    [Trait("Release", "20.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
    public void Should_store_given_configurations_when_provided_file_configuration_object()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        var configuration = GivenCombinedFileConfigurationObject();

        // Act
        _configRoot = new ConfigurationBuilder()
            .AddOcelot(configuration, primaryConfigFileName, false, false)
            .Build();

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(true, configuration);
    }

    [Fact]
    public void Should_merge_files_except_env()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        GivenMultipleConfigurationFiles(TestID, true);

        // Act
        WhenIAddOcelotConfiguration(TestID);

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        NotContainsEnvSpecificConfig();
    }

    [Fact]
    public void Should_merge_files_in_specific_folder()
    {
        // Arrange
        GivenMultipleConfigurationFiles(TestID);

        // Act
        WhenIAddOcelotConfiguration(TestID);

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
    }

    [Fact]
    [Trait("Bug", "1216")] // https://github.com/ThreeMammals/Ocelot/issues/1216
    [Trait("PR", "1227")] // https://github.com/ThreeMammals/Ocelot/pull/1227
    public void Should_merge_files_to_memory()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        GivenMultipleConfigurationFiles(TestID);

        // Act
        WhenIAddOcelotConfiguration(TestID, MergeOcelotJson.ToMemory);

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        TheOcelotPrimaryConfigFileExists(false);
    }

    [Fact]
    [Trait("Bug", "1518")] // https://github.com/ThreeMammals/Ocelot/issues/1518
    [Trait("PR", "1986")] // https://github.com/ThreeMammals/Ocelot/pull/1986
    public void Should_merge_files_with_null_environment()
    {
        // Arrange
        environmentConfigFileName = null; // Ups!
        const IWebHostEnvironment NullEnvironment = null; // Wow!
        GivenMultipleConfigurationFiles(TestID, false);

        // Act
        _configRoot = new ConfigurationBuilder()
            .AddOcelot(TestID, NullEnvironment, MergeOcelotJson.ToMemory, primaryConfigFileName, globalConfigFileName, environmentConfigFileName, false, false)
            .Build();

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        TheOcelotPrimaryConfigFileExists(false);
    }

    [Fact]
    [Trait("Bug", "2084")] // https://github.com/ThreeMammals/Ocelot/issues/2084
    [Trait("PR", "2120")] // https://github.com/ThreeMammals/Ocelot/pull/2120
    public void Should_use_relative_path_for_global_config()
    {
        // Arrange
        GivenMultipleConfigurationFiles(TestID);

        // Act
        WhenIAddOcelotConfigurationWithDefaultFilePaths(TestID);

        // Assert
        var config = ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        Assert.NotNull(config);
        Assert.Equal(TestName(), config.GlobalConfiguration.RequestIdKey);
    }

    private void WhenIAddOcelotConfiguration(string folder, MergeOcelotJson mergeOcelotJson = MergeOcelotJson.ToFile)
    {
        _configRoot = new ConfigurationBuilder()
            .AddOcelot(folder, _hostingEnvironment.Object, mergeOcelotJson, primaryConfigFileName, globalConfigFileName, environmentConfigFileName, false, false)
            .Build();
    }

    private void WhenIAddOcelotConfigurationWithDefaultFilePaths(string folder, MergeOcelotJson mergeOcelotJson = MergeOcelotJson.ToFile)
    {
        _configRoot = new ConfigurationBuilder()
            .AddOcelot(folder, _hostingEnvironment.Object, mergeOcelotJson, optional: false, reloadOnChange: false)
            .Build();
    }

    private FileConfiguration ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(bool useCombinedConfig, FileConfiguration combined = null)
    {
        var fc = _configRoot.Get<FileConfiguration>();

        fc.GlobalConfiguration.BaseUrl.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.BaseUrl : _globalConfig.GlobalConfiguration.BaseUrl);
        fc.GlobalConfiguration.RateLimitOptions.ClientIdHeader.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.ClientIdHeader : _globalConfig.GlobalConfiguration.RateLimitOptions.ClientIdHeader);
        fc.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders : _globalConfig.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders);
        fc.GlobalConfiguration.RateLimitOptions.EnableHeaders.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.EnableHeaders : _globalConfig.GlobalConfiguration.RateLimitOptions.EnableHeaders);
        fc.GlobalConfiguration.RateLimitOptions.HttpStatusCode.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.HttpStatusCode : _globalConfig.GlobalConfiguration.RateLimitOptions.HttpStatusCode);
        fc.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage : _globalConfig.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage);
        fc.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix : _globalConfig.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix);
        fc.GlobalConfiguration.RequestIdKey.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.RequestIdKey : _globalConfig.GlobalConfiguration.RequestIdKey);
        fc.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.ServiceDiscoveryProvider.Scheme : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
        fc.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.ServiceDiscoveryProvider.Host : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host);
        fc.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.ServiceDiscoveryProvider.Port : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Port);
        fc.GlobalConfiguration.ServiceDiscoveryProvider.Type.ShouldBe(useCombinedConfig ? combined.GlobalConfiguration.ServiceDiscoveryProvider.Type : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Type);

        fc.Routes.Count.ShouldBe(useCombinedConfig ? combined.Routes.Count : _routeA.Routes.Count + _routeB.Routes.Count);

        fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? combined.Routes[0].DownstreamPathTemplate : _routeA.Routes[0].DownstreamPathTemplate));
        fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? combined.Routes[1].DownstreamPathTemplate : _routeB.Routes[0].DownstreamPathTemplate));
        fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? combined.Routes[2].DownstreamPathTemplate : _routeB.Routes[1].DownstreamPathTemplate));

        fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? combined.Routes[0].DownstreamScheme : _routeA.Routes[0].DownstreamScheme));
        fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? combined.Routes[1].DownstreamScheme : _routeB.Routes[0].DownstreamScheme));
        fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? combined.Routes[2].DownstreamScheme : _routeB.Routes[1].DownstreamScheme));

        fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? combined.Routes[0].Key : _routeA.Routes[0].Key));
        fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? combined.Routes[1].Key : _routeB.Routes[0].Key));
        fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? combined.Routes[2].Key : _routeB.Routes[1].Key));

        fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? combined.Routes[0].UpstreamHost : _routeA.Routes[0].UpstreamHost));
        fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? combined.Routes[1].UpstreamHost : _routeB.Routes[0].UpstreamHost));
        fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? combined.Routes[2].UpstreamHost : _routeB.Routes[1].UpstreamHost));

        fc.Aggregates.Count.ShouldBe(useCombinedConfig ? combined.Aggregates.Count : _aggregate.Aggregates.Count);
        return fc;
    }

    private void NotContainsEnvSpecificConfig()
    {
        var fc = _configRoot.Get<FileConfiguration>();
        fc.Routes.ShouldNotContain(x => x.DownstreamScheme == _envSpecific.Routes[0].DownstreamScheme);
        fc.Routes.ShouldNotContain(x => x.DownstreamPathTemplate == _envSpecific.Routes[0].DownstreamPathTemplate);
        fc.Routes.ShouldNotContain(x => x.Key == _envSpecific.Routes[0].Key);
    }

    [Collection(nameof(SequentialTests))]
    public class Sequential : ConfigurationBuilderExtensionsTestsBase
    {
        public Sequential()
        {
            // TestID in-name files are for parallel execution, but the current directory will be a context (aka "." folder)
            files.RemoveAll(f => f.Contains(TestID));
        }

        /// <summary>
        /// Test for AddOcelot method without folder argument: 
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, IWebHostEnvironment, MergeOcelotJson, string, string, string, bool?, bool?)" />
        /// </summary>
        [Theory]
        //[Theory(DisableDiscoveryEnumeration = true)]
        [InlineData(MergeOcelotJson.ToMemory, false)]
        [InlineData(MergeOcelotJson.ToFile, true)]
        public void AddOcelot_IWebHostEnvironment_MergeOcelotJson_WithoutFolderArgument_WithEnvironmentAndMergeOption_ToMemory(MergeOcelotJson mergeTo, bool primaryFileExists)
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            GivenMultipleConfigurationFiles(Environment.CurrentDirectory); // implicit current folder aka "."
            primaryConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile); // will be created after merging
            globalConfigFileName = files.Single(f => f.Contains(ConfigurationBuilderExtensions.GlobalConfigFile));
            files.Add(primaryConfigFileName); // to be deleted

            // Act
            var configRoot = new ConfigurationBuilder()
                .AddOcelot(_hostingEnvironment.Object, mergeTo, primaryConfigFileName, globalConfigFileName, environmentConfigFileName, false, false)
                .Build();

            // Assert
            Assert.NotNull(configRoot);
            var actual = configRoot.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
            TheOcelotPrimaryConfigFileExists(primaryFileExists);
            if (primaryFileExists)
            {
                var fileContent = File.ReadAllText(primaryConfigFileName);
                Assert.Contains(TestName(), fileContent);
            }
        }

        /// <summary>
        /// Test for AddOcelot method without folder argument: 
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, IWebHostEnvironment, MergeOcelotJson, string, string, string, bool?, bool?)" />
        /// </summary>
        [Fact]
        public void AddOcelot_IWebHostEnvironment_MergeOcelotJson_WithoutFolderArgument_WithDefaults()
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            GivenMultipleConfigurationFiles(Environment.CurrentDirectory); // implicit current folder aka "."
            primaryConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile); // will be created after merging
            globalConfigFileName = files.Single(f => f.Contains(ConfigurationBuilderExtensions.GlobalConfigFile));
            files.Add(primaryConfigFileName); // to be deleted

            // Act - using default parameters (ToFile, no custom file names, no optional/reloadOnChange overrides)
            var configRoot = new ConfigurationBuilder()
                .AddOcelot(_hostingEnvironment.Object, MergeOcelotJson.ToFile)
                .Build();

            // Assert
            Assert.NotNull(configRoot);
            var actual = configRoot.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
            TheOcelotPrimaryConfigFileExists();
            Assert.True(actual.Routes.Count > 0);
        }

        #region ---------< TESTS FOR AddOcelot WITH FileConfiguration >--------
        /// <summary>
        /// Test for AddOcelot method with FileConfiguration and IWebHostEnvironment:
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, FileConfiguration, IWebHostEnvironment, MergeOcelotJson, string, string, string, bool?, bool?)" />
        /// </summary>
        [Theory]
        [InlineData(MergeOcelotJson.ToMemory, false)]
        [InlineData(MergeOcelotJson.ToFile, true)]
        public void AddOcelot_FileConfiguration_IWebHostEnvironment_MergeOcelotJson_WithFileConfigurationAndEnvironment_MergedTo(MergeOcelotJson mergeTo, bool primaryFileExists)
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            var configuration = GivenCombinedFileConfigurationObject();
            primaryConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile); // will be created after merging
            globalConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.GlobalConfigFile);
            files.Add(primaryConfigFileName); // to be deleted

            // Act
            var configRoot = new ConfigurationBuilder()
                .AddOcelot(configuration, _hostingEnvironment.Object, mergeTo, primaryConfigFileName, globalConfigFileName, environmentConfigFileName, false, false)
                .Build();

            // Assert
            Assert.NotNull(configRoot);
            var actual = configRoot.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(configuration.GlobalConfiguration.BaseUrl, actual.GlobalConfiguration.BaseUrl);
            Assert.Equal(configuration.Routes.Count, actual.Routes.Count);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
            TheOcelotPrimaryConfigFileExists(primaryFileExists);
        }

        /// <summary>
        /// Test for AddOcelot method with FileConfiguration and IWebHostEnvironment:
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, FileConfiguration, IWebHostEnvironment, MergeOcelotJson, string, string, string, bool?, bool?)" />
        /// </summary>
        [Fact]
        public void AddOcelot_FileConfiguration_IWebHostEnvironment_MergeOcelotJson_WithFileConfigurationAndEnvironment_WithoutOptionalArgs()
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            var configuration = GivenCombinedFileConfigurationObject();
            primaryConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile); // will be created after merging
            files.Add(primaryConfigFileName); // to be deleted

            // Act - using default MergeOcelotJson.ToFile
            var configRoot = new ConfigurationBuilder()
                .AddOcelot(configuration, _hostingEnvironment.Object, MergeOcelotJson.ToFile)
                .Build();

            // Assert
            Assert.NotNull(configRoot);
            var actual = configRoot.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(configuration.GlobalConfiguration.BaseUrl, actual.GlobalConfiguration.BaseUrl);
            Assert.Equal(configuration.Routes.Count, actual.Routes.Count);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
            TheOcelotPrimaryConfigFileExists();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void AddOcelot_FileConfiguration_IWebHostEnvironment_MergeOcelotJson_WithFileConfigurationAndEnvironment_CustomOptionalArgs(bool optional, bool reloadOnChange)
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            var configuration = GivenCombinedFileConfigurationObject();
            primaryConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.PrimaryConfigFile); // will be created after merging
            globalConfigFileName = Path.Combine(Environment.CurrentDirectory, ConfigurationBuilderExtensions.GlobalConfigFile);
            files.Add(primaryConfigFileName); // to be deleted

            // Act - with optional=true and reloadOnChange=true
            var configRoot = new ConfigurationBuilder()
                .AddOcelot(configuration, _hostingEnvironment.Object, MergeOcelotJson.ToFile, primaryConfigFileName, globalConfigFileName, environmentConfigFileName, optional, reloadOnChange)
                .Build();

            // Assert
            Assert.NotNull(configRoot);
            var actual = configRoot.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(configuration.GlobalConfiguration.BaseUrl, actual.GlobalConfiguration.BaseUrl);
            Assert.Equal(configuration.Routes.Count, actual.Routes.Count);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
            TheOcelotPrimaryConfigFileExists();
        }
        #endregion
        #region ---------< TESTS FOR AddOcelot WITH SINGLE REQUIRED 'builder' ARGUMENT >---------
        /// <summary>
        /// Test for AddOcelot method with single required 'builder' argument:
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, string, bool?, bool?)" />
        /// </summary>
        [Fact]
        public void AddOcelot_IConfigurationBuilder_string_bool_bool_OnlyBuilderArgument_WithoutOptionalArguments()
        {
            // Arrange
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            FileConfiguration configuration = new();
            configuration.GlobalConfiguration.RequestIdKey = TestName();
            primaryConfigFileName = CreateConfigFile(Environment.CurrentDirectory, string.Empty, configuration, ConfigurationBuilderExtensions.PrimaryConfigFile);

            // Act
            var root = new ConfigurationBuilder()
                .AddOcelot() // only builder argument, no optional arguments
                .Build();

            // Assert
            TheOcelotPrimaryConfigFileExists();
            Assert.NotNull(root);
            var actual = root.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
        }

        /// <summary>
        /// Test for AddOcelot method with single required 'builder' argument:
        /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, string, bool?, bool?)" />
        /// </summary>
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void AddOcelot_IConfigurationBuilder_string_bool_bool_NoPrimaryFile_WithAllOptionalArguments(bool optional, bool reloadOnChange)
        {
            GivenTheEnvironmentIs(Environment.CurrentDirectory);
            FileConfiguration configuration = new();
            configuration.GlobalConfiguration.RequestIdKey = TestName();
            primaryConfigFileName = CreateConfigFile(Environment.CurrentDirectory, string.Empty, configuration, ConfigurationBuilderExtensions.PrimaryConfigFile);

            // Act
            var root = new ConfigurationBuilder()
                .AddOcelot(primaryFile: null, optional, reloadOnChange) // with optional, reloadOnChange
                .Build();

            // Assert
            TheOcelotPrimaryConfigFileExists();
            Assert.NotNull(root);
            var actual = root.Get<FileConfiguration>();
            Assert.NotNull(actual?.GlobalConfiguration);
            Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
        }
    }

    // Parallel tests below

    /// <summary>
    /// Test for AddOcelot method with single required 'builder' argument:
    /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, string, bool?, bool?)" />
    /// </summary>
    [Fact]
    public void AddOcelot_IConfigurationBuilder_string_bool_bool_WithCustomPrimaryFile_NoOptionalArguments()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        FileConfiguration configuration = new();
        configuration.GlobalConfiguration.RequestIdKey = TestName();
        primaryConfigFileName = CreateConfigFile(TestID, string.Empty, configuration, ConfigurationBuilderExtensions.PrimaryConfigFile);

        // Act
        var root = new ConfigurationBuilder()
            .AddOcelot(primaryConfigFileName) // with custom primary file, no optional arguments
            .Build();

        // Assert
        TheOcelotPrimaryConfigFileExists();
        Assert.NotNull(root);
        var actual = root.Get<FileConfiguration>();
        Assert.NotNull(actual?.GlobalConfiguration);
        Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
    }

    /// <summary>
    /// Test for AddOcelot method with single required 'builder' argument:
    /// <see cref="ConfigurationBuilderExtensions.AddOcelot(IConfigurationBuilder, string, bool?, bool?)" />
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AddOcelot_IConfigurationBuilder_string_bool_bool_WithAllOptionalArguments(bool optional, bool reloadOnChange)
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        FileConfiguration configuration = new();
        configuration.GlobalConfiguration.RequestIdKey = TestName();
        primaryConfigFileName = CreateConfigFile(TestID, string.Empty, configuration, ConfigurationBuilderExtensions.PrimaryConfigFile);

        // Act
        var root = new ConfigurationBuilder()
            .AddOcelot(primaryConfigFileName, optional, reloadOnChange) // with optional, reloadOnChange
            .Build();

        // Assert
        TheOcelotPrimaryConfigFileExists();
        Assert.NotNull(root);
        var actual = root.Get<FileConfiguration>();
        Assert.NotNull(actual?.GlobalConfiguration);
        Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);
    }
    #endregion

    [Fact]
    public void GetMergedOcelotJson_WhenMultipleFilesExist_ShouldSkipPrimaryConfigFileDuringMerge()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);

        // Create multiple config files including the primary one
        var primaryConfig = new FileConfiguration { Routes = [GetRoute("Primary")] };
        var primaryFile = CreateConfigFile(TestID, "primary", primaryConfig);
        GivenMultipleConfigurationFiles(TestID);
        var builder = new ConfigurationBuilder();

        // Act
        var json = builder.GetMergedOcelotJson(TestID, _hostingEnvironment.Object,
            fileConfiguration: null, primaryFile, null, null);

        // Assert
        var actual = JsonConvert.DeserializeObject<FileConfiguration>(json);
        Assert.Equal(TestName(), actual.GlobalConfiguration.RequestIdKey);

        // The primary config file (ocelot.json) should be skipped during merge when multiple files exist
        // So the "Primary" route should NOT appear in the merged configuration
        Assert.DoesNotContain(actual.Routes, x => x.Key == "Primary");

        // But routes from other sub-configurations should be present
        Assert.Contains(actual.Routes, x => x.Key == "A");
        Assert.Contains(actual.Routes, x => x.Key == "B");
    }

    [Fact]
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public void GetMergedOcelotJson_GuardSchema_WhenInvalidSchema_ShouldInitializeTopLevelCollections()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        var configuration = new FileConfiguration
        {
            Aggregates = null,
            Routes = null,
            DynamicRoutes = null,
            GlobalConfiguration = null,
        };
        var builder = new ConfigurationBuilder();

        // Act
        var json = builder.GetMergedOcelotJson(TestID, _hostingEnvironment.Object, fileConfiguration: configuration);

        // Assert
        Assert.NotEmpty(json);
        Assert.NotNull(configuration.Aggregates);
        Assert.NotNull(configuration.Routes);
        Assert.NotNull(configuration.DynamicRoutes);
        Assert.NotNull(configuration.GlobalConfiguration);
        dynamic actual = JObject.Parse(json);
        Assert.NotNull(actual.GlobalConfiguration);
        Assert.IsType<JObject>(actual.GlobalConfiguration);
        AssertJArray(actual.Aggregates);
        AssertJArray(actual.Routes);
        AssertJArray(actual.DynamicRoutes);
        static void AssertJArray(dynamic obj)
        {
            Assert.NotNull(obj);
            Assert.IsType<JArray>(obj);
            JArray arr = obj as JArray;
            Assert.Empty(arr);
        }
    }

    [Fact]
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public void AddOcelot_JObject_SerializedToFile()
    {
        // Arrange
        GivenTheEnvironmentIs(TestID);
        var configuration = GivenCombinedFileConfigurationObject();
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        JObject jobjConfiguration = JObject.Parse(json);

        // Act
        _configRoot = new ConfigurationBuilder()
            .AddOcelot(jobjConfiguration, primaryConfigFileName, null, null)
            .Build();

        // Assert
        ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(true, configuration);
        TheOcelotPrimaryConfigFileExists();
        var actual = _configRoot["GlobalConfiguration:RequestIdKey"];
        Assert.Equal(TestName(), actual);
    }

    [Fact]
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public void OcelotJMergeSection_WhenSourceSectionIsNeitherJObjectNorJArray_DoesNothingAndReturnsTargetUnchanged()
    {
        // Arrange
        var target = JObject.Parse(@"
        {
          ""Routes"": [
            { ""Key"": ""route1"" }
          ],
          ""GlobalConfiguration"": {
            ""BaseUrl"": ""https://ocelot.net""
          }
        }");

        var source = JObject.Parse(@"
        {
          ""Routes"": ""this-is-a-string-not-array-or-object"",  // Deliberately not JArray / JObject
          ""GlobalConfiguration"": 12345                         // Deliberately not JObject / JArray
        }");

        // Act
        var result = target.OcelotJMergeSection(source, "Routes");
        result = result.OcelotJMergeSection(source, "GlobalConfiguration");

        // Assert
        Assert.Same(target, result); // Should return the original target (fluent)

        // Routes section should remain unchanged
        var routesAfter = target.OcelotJSection("Routes") as JArray;
        Assert.NotNull(routesAfter);
        Assert.Single(routesAfter);
        Assert.Equal("route1", routesAfter[0]["Key"].Value<string>());

        // GlobalConfiguration section should remain unchanged
        var globalAfter = target.OcelotJSection("GlobalConfiguration") as JObject;
        Assert.NotNull(globalAfter);
        Assert.Equal("https://ocelot.net", globalAfter["BaseUrl"].Value<string>());
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJSection : UnitTest
    {
        [Fact]
        public void OcelotJSection_AllLogicalBranches()
        {
            const string myProp = "MyProp";

            // Scenario 1: obj var is null after casting to JObject
            string json = null;
            JToken token = null; // JToken.Parse(jsonInput);
            var result = token.OcelotJSection(myProp);
            Assert.Null(result);

            // Scenario 2: obj not null but property not found
            json = "{}";
            token = JToken.Parse(json);
            result = token.OcelotJSection(myProp);
            Assert.Null(result);

            // Scenario 3: property found but value is null
            json = @"{""MyProp"": null}";
            token = JToken.Parse(json);
            result = token.OcelotJSection(myProp);
            Assert.NotNull(result);
            Assert.Equal(null, result.Value<int?>());

            // Scenario 4: property found with integer value
            json = @"{""MyProp"": 123}";
            token = JToken.Parse(json);
            result = token.OcelotJSection(myProp);
            Assert.NotNull(result);
            Assert.Equal(123, result.Value<int>());
        }

        [Fact]
        public void OcelotJSection_CaseInsensitiveLookup_WorksCorrectly()
        {
            // Arrange
            var token = JObject.Parse(@"{ ""routes"": [1,2,3], ""GLOBALCONFIGURATION"": { ""BaseUrl"": ""https://ocelot.net"" } }");

            // Act & Assert
            Assert.NotNull(token.OcelotJSection("Routes"));
            Assert.NotNull(token.OcelotJSection("routes"));
            Assert.NotNull(token.OcelotJSection("ROUTES"));
            Assert.NotNull(token.OcelotJSection("GlobalConfiguration"));
            Assert.NotNull(token.OcelotJSection("globalconfiguration"));
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJSetSection : UnitTest
    {
        [Fact]
        public void OcelotJSetSection_NullTarget_DoesNothing()
        {
            JToken to = null;

            // Act
            to.OcelotJSetSection(TestName(), JValue.CreateString(TestID));

            Assert.Null(to); // Still null, no exception
        }

        [Fact]
        public void OcelotJSetSection_NonJObjectTarget_DoesNothing()
        {
            var array = new JArray { 1, 2, 3 };

            // Act
            array.OcelotJSetSection(TestName(), JValue.CreateString(TestID));

            Assert.Equal(3, array.Count);
            Assert.Contains(1, array);
            Assert.Contains(2, array);
            Assert.Contains(3, array);
            var jtoken = array.OcelotJSection(TestName());
            Assert.Null(jtoken);
        }

        [Theory]
        [InlineData("section")]
        [InlineData("SECTION")]
        [InlineData("SeCtIoN")]
        public void OcelotJSetSection_ExistingProperty_UpdatesValue_CaseInsensitive(string propertyName)
        {
            var jObj = JObject.Parse("""{ "section": "oldValue", "other": 42 }""");

            // Act
            jObj.OcelotJSetSection(propertyName, JValue.CreateString(TestID));

            Assert.Equal(TestID, (string)jObj["section"]);
            Assert.Equal(42, (int)jObj["other"]); // Other properties unchanged
        }

        [Fact]
        public void OcelotJSetSection_NewProperty_AddsIt()
        {
            var jObj = new JObject();
            var newSection = TestName();

            // Act
            jObj.OcelotJSetSection(newSection, JValue.CreateString(TestID));

            Assert.Equal(TestID, (string)jObj[newSection]);
            Assert.Single(jObj.Properties());
        }

        [Fact]
        public void OcelotJSetSection_UpdatesComplexValue()
        {
            var jObj = JObject.Parse("""{ "config": { "enabled": false } }""");
            var newValue = JObject.Parse("""{ "enabled": true, "timeout": 30 }""");

            // Act
            jObj.OcelotJSetSection("config", newValue);

            Assert.True((bool)jObj["config"]["enabled"]!);
            Assert.Equal(30, (int)jObj["config"]["timeout"]!);
        }

        [Fact]
        public void OcelotJSetSection_NullValue_SetsNull()
        {
            var jObj = JObject.Parse("""{ "data": "something" }""");

            // Act
            jObj.OcelotJSetSection("data", null);

            var actual = jObj["data"];
            Assert.NotNull(actual);
            Assert.True(((JValue)actual).Type == JTokenType.Null);
            Assert.Null(actual.Value<string>());
        }

        [Fact]
        public void OcelotJSetSection_EmptyName_AddsProperty()
        {
            var jObj = new JObject();

            // Act
            jObj.OcelotJSetSection("", JValue.CreateString("emptyNameValue"));

            Assert.Equal("emptyNameValue", (string)jObj[""]);
        }

        [Fact]
        public void OcelotJSetSection_NullName_ThrowsOrHandlesGracefully()
        {
            var jObj = new JObject();

            // Newtonsoft.Json behavior: Add with null name throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                jObj.OcelotJSetSection(null, JValue.CreateString("value")));
        }

        [Theory]
        [InlineData("value", JTokenType.String)]
        [InlineData(123, JTokenType.Integer)]
        [InlineData(true, JTokenType.Boolean)]
        [InlineData(null, JTokenType.Null)]
        public void OcelotJSetSection_AcceptsVariousValueTypes(object rawValue, JTokenType expectedType)
        {
            var jObj = new JObject();
            JToken value = rawValue is null ? null : JToken.FromObject(rawValue);

            // Act
            jObj.OcelotJSetSection(TestName(), value);

            var actual = jObj[TestName()];
            Assert.NotNull(actual);
            Assert.Equal(expectedType, actual.Type);
        }
    }
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJ : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;

        [Fact]
        public void OcelotJson_ReturnsJsonFromBuilder()
        {
            // Arrange
            var testJson = @"{ ""Routes"": [] }";
            _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJsonKey] = testJson;

            // Act
            var actual = _builder.OcelotJson();

            // Assert
            Assert.Equal(testJson, actual);
        }

        [Fact]
        public void OcelotJObject_ReturnsJObjectFromBuilder()
        {
            // Arrange
            var jobj = new JObject { { "Routes", new JArray() } };
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJObject();

            // Assert
            Assert.Same(jobj, actual);
        }

        [Fact]
        public void OcelotJRoutes_ReturnsRoutesArray()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Routes"": [{ ""Key"": ""route1"" }] }");
            GivenJObject(jobj);

            // Act
            JArray actual = _builder.OcelotJRoutes();

            // Assert
            Assert.NotNull(actual);
            Assert.Single(actual);
        }

        [Fact]
        public void OcelotJDynamicRoutes_ReturnsDynamicRoutesArray()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""DynamicRoutes"": [{ ""ServiceName"": ""service1"" }] }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJDynamicRoutes();

            // Assert
            Assert.NotNull(actual);
            Assert.Single(actual);
        }

        [Fact]
        public void OcelotJAggregates_ReturnsAggregatesArray()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Aggregates"": [{ ""UpstreamPathTemplate"": ""/agg"" }] }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJAggregates();

            // Assert
            Assert.NotNull(actual);
            Assert.Single(actual);
        }

        [Fact]
        public void OcelotJGlobalConfiguration_ReturnsGlobalConfigObject()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""GlobalConfiguration"": { ""BaseUrl"": ""https://ocelot.net"" } }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJGlobalConfiguration();

            // Assert
            Assert.NotNull(actual);
            Assert.Equal("https://ocelot.net", actual["BaseUrl"].Value<string>());
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJRoute : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;
        private const string UpstreamPathTemplate = nameof(FileRoute.UpstreamPathTemplate);
        private const string DownstreamPathTemplate = nameof(FileRoute.DownstreamPathTemplate);
        private const string Key = nameof(FileRoute.Key);

        [Fact]
        public void OcelotJRoute_FindByUpstreamPathTemplate_ReturnsMatchingRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""DownstreamPathTemplate"": ""/users"", ""Key"": ""route1"" },
                { ""UpstreamPathTemplate"": ""/api/orders"", ""DownstreamPathTemplate"": ""/orders"", ""Key"": ""route2"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/api/orders");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("/api/orders", route[UpstreamPathTemplate].Value<string>());
            Assert.Equal("/orders", route[DownstreamPathTemplate].Value<string>());
            Assert.Equal("route2", route[Key].Value<string>());
        }

        [Fact]
        public void OcelotJRoute_FindByKey_ReturnsMatchingRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""GetUsers"" },
                { ""UpstreamPathTemplate"": ""/api/orders"", ""Key"": ""GetOrders"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute(key: "GetUsers");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("GetUsers", route[Key].Value<string>());
            Assert.Equal("/api/users", route[UpstreamPathTemplate].Value<string>());
        }

        [Fact]
        public void OcelotJRoute_NoMatch_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""route1"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/api/nonexistent");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJRoute_EmptyRoutes_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Routes"": [] }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/api/nonexistent");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJRoute_CaseSensitiveComparison_FindsExactMatch()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/API/Users"", ""Key"": ""route1"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/API/Users");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("/API/Users", route[UpstreamPathTemplate].Value<string>());
            Assert.Equal("route1", route[Key].Value<string>());
        }

        [Fact]
        public void OcelotJRoute_CaseInsensitiveComparison_IgnoresCase()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""route1"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/API/USERS");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("/api/users", route[UpstreamPathTemplate].Value<string>());
        }

        [Fact]
        public void OcelotJRoute_NullRoutesArray_HandleGracefully()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Routes"": null }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJRoute("/api/nonexistent");

            // Assert
            Assert.Null(route);
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJDynamicRoute : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;
        private const string ServiceName = nameof(FileDynamicRoute.ServiceName);
        private const string ServiceNamespace = nameof(FileDynamicRoute.ServiceNamespace);
        private const string Key = nameof(FileDynamicRoute.Key);

        [Fact]
        public void OcelotJDynamicRoute_FindByServiceName_NoNamespace_ReturnsMatchingRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""DynamicRoutes"": [
                { ""ServiceName"": ""UserService"", ""ServiceNamespace"": """", ""Key"": ""dynamicRoute1"" },
                { ""ServiceName"": ""OrderService"", ""ServiceNamespace"": """", ""Key"": ""dynamicRoute2"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute("UserService");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("UserService", route[ServiceName].Value<string>());
            Assert.Equal("dynamicRoute1", route[Key].Value<string>());
        }

        [Fact]
        public void OcelotJDynamicRoute_FindByServiceNameAndNamespace_ReturnsMatchingRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""DynamicRoutes"": [
                { ""ServiceName"": ""UserService"", ""ServiceNamespace"": ""v1"", ""Key"": ""dynamicRoute1"" },
                { ""ServiceName"": ""UserService"", ""ServiceNamespace"": ""v2"", ""Key"": ""dynamicRoute2"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute("UserService", "v1");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("UserService", route[ServiceName].Value<string>());
            Assert.Equal("v1", route[ServiceNamespace].Value<string>());
            Assert.Equal("dynamicRoute1", route[Key].Value<string>());
        }

        [Fact]
        public void OcelotJDynamicRoute_FindByKey_ReturnsMatchingRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""DynamicRoutes"": [
                { ""ServiceName"": ""UserService"", ""Key"": ""GetUsers"" },
                { ""ServiceName"": ""OrderService"", ""Key"": ""GetOrders"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute(key: "GetUsers");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("GetUsers", route[Key].Value<string>());
        }

        [Fact]
        public void OcelotJDynamicRoute_NoMatch_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""DynamicRoutes"": [
                { ""ServiceName"": ""UserService"", ""Key"": ""route1"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute("NonexistentService");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJDynamicRoute_NoDynamicRoutes_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""DynamicRoutes"": null }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute("NonexistentService");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJDynamicRoute_CaseSensitiveComparison_FindsExactMatch()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""DynamicRoutes"": [
                { ""ServiceName"": ""UserService"", ""Key"": ""route1"" }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJDynamicRoute("UserService", comparison: StringComparison.Ordinal);

            // Assert
            Assert.NotNull(route);
            Assert.Equal("UserService", route[ServiceName].Value<string>());

            // Act
            route = _builder.OcelotJDynamicRoute("USERservice", comparison: StringComparison.OrdinalIgnoreCase);

            // Assert
            Assert.NotNull(route);
            Assert.Equal("UserService", route[ServiceName].Value<string>());
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJAggregate : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;
        private const string UpstreamPathTemplate = nameof(FileAggregateRoute.UpstreamPathTemplate);

        [Fact]
        public void OcelotJAggregate_FindByUpstreamPathTemplate_ReturnsMatchingAggregate()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Aggregates"": [
                { ""UpstreamPathTemplate"": ""/aggregated/users"", ""RouteKeys"": [""u1"", ""u2"" ] },
                { ""UpstreamPathTemplate"": ""/aggregated/orders"", ""RouteKeys"": [""o1"", ""o2"" ] }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/aggregated/users");

            // Assert
            Assert.NotNull(route);
            Assert.Equal("/aggregated/users", route[UpstreamPathTemplate].Value<string>());
        }

        [Fact]
        public void OcelotJAggregate_NoMatch_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Aggregates"": [
                { ""UpstreamPathTemplate"": ""/aggregated/users"", ""RouteKeys"": [""u1"", ""u2"" ] }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/aggregated/nonexistent");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJAggregate_EmptyAggregates_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Aggregates"": [] }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/aggregated/users");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJAggregate_CaseInsensitiveComparison_IgnoresCase()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Aggregates"": [
                { ""UpstreamPathTemplate"": ""/aggregated/users"", ""RouteKeys"": [""u1"", ""u2"" ] }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/AGGREGATED/USERS");

            // Assert
            Assert.NotNull(route);
        }

        [Fact]
        public void OcelotJAggregate_CaseSensitiveComparison_FindsExactMatch()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Aggregates"": [
                { ""UpstreamPathTemplate"": ""/aggregated/Users"", ""RouteKeys"": [""u1"", ""u2"" ] }
              ]
            }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/aggregated/Users", StringComparison.Ordinal);

            // Assert
            Assert.NotNull(route);
            Assert.Equal("/aggregated/Users", route[nameof(FileAggregateRoute.UpstreamPathTemplate)].Value<string>());
        }

        [Fact]
        public void OcelotJAggregate_NullAggregatesArray_HandleGracefully()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Aggregates"": null }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate("/aggregated/nonexistent");

            // Assert
            Assert.Null(route);
        }

        [Fact]
        public void OcelotJAggregate_NullUpstreamPathTemplate_HandleGracefully()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""Aggregates"": [] }");
            GivenJObject(jobj);

            // Act
            var route = _builder.OcelotJAggregate(null);

            // Assert
            Assert.Null(route);
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJGlobalProperty : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;
        private const string UpstreamPathTemplate = nameof(FileAggregateRoute.UpstreamPathTemplate);

        [Fact]
        public void OcelotJGlobalProperty_PropertyExists_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""GlobalConfiguration"": {
                ""BaseUrl"": ""https://ocelot.net"",
                ""RequestIdKey"": ""OcelotRequestId""
              }
            }");
            GivenJObject(jobj);

            // Act
            var baseUrl = _builder.OcelotJGlobalProperty<string>("BaseUrl");

            // Assert
            Assert.NotNull(baseUrl);
            Assert.Equal("https://ocelot.net", baseUrl);
        }

        [Fact]
        public void OcelotJGlobalProperty_NestedPropertyPath_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""GlobalConfiguration"": {
                ""RateLimitOptions"": {
                  ""ClientIdHeader"": ""X-Client-Id"",
                  ""EnableHeaders"": true
                }
              }
            }");
            GivenJObject(jobj);

            // Act
            var clientIdHeader = _builder.OcelotJGlobalProperty<string>("RateLimitOptions.ClientIdHeader");

            // Assert
            Assert.NotNull(clientIdHeader);
            Assert.Equal("X-Client-Id", clientIdHeader);
        }

        [Fact]
        public void OcelotJGlobalProperty_PropertyNotFound_ReturnsDefault()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""GlobalConfiguration"": { ""BaseUrl"": ""https://ocelot.net"" } }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJGlobalProperty<string>("NonexistentProperty");

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJGlobalProperty_NullValue_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""GlobalConfiguration"": { ""BaseUrl"": null } }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJGlobalProperty<string>("BaseUrl");

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJGlobalProperty_IntegerProperty_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""GlobalConfiguration"": {
                ""ServiceDiscoveryProvider"": {
                  ""Port"": 8500
                }
              }
            }");
            GivenJObject(jobj);

            // Act
            int port = _builder.OcelotJGlobalProperty<int>("ServiceDiscoveryProvider.Port");

            // Assert
            Assert.Equal(8500, port);
        }

        [Fact]
        public void OcelotJGlobalProperty_BooleanProperty_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""GlobalConfiguration"": {
                ""RateLimitOptions"": {
                  ""EnableHeaders"": true
                }
              }
            }");
            GivenJObject(jobj);

            // Act
            bool enableHeaders = _builder.OcelotJGlobalProperty<bool>("RateLimitOptions.EnableHeaders");

            // Assert
            Assert.True(enableHeaders);
        }

        [Fact]
        public void OcelotJGlobalProperty_GlobalConfigurationNull_HandleGracefully()
        {
            // Arrange
            var jobj = JObject.Parse(@"{ ""GlobalConfiguration"": null }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJGlobalProperty<string>("BaseUrl");

            // Assert
            Assert.Null(actual);
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJProperty : UnitTest
    {
        private readonly ConfigurationBuilder _builder = new();
        private void GivenJObject(JObject jobj) => _builder.Properties[ConfigurationBuilderExtensions.MergedConfigJObjectKey] = jobj;

        [Fact]
        public void OcelotJProperty_PropertyFoundInRoute_ReturnsRouteProperty()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""TimeoutInMilliseconds"": 5000 }
              ],
              ""GlobalConfiguration"": {
                ""TimeoutInMilliseconds"": 30000
              }
            }");
            GivenJObject(jobj);

            // Act
            var timeout = _builder.OcelotJProperty<int>("TimeoutInMilliseconds", "/api/users");

            // Assert
            Assert.Equal(5000, timeout);
        }

        [Fact]
        public void OcelotJProperty_PropertyNotFoundInRoute_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""GetUsers"" }
              ],
              ""GlobalConfiguration"": {
                ""RequestIdKey"": ""OcelotRequestId""
              }
            }");
            GivenJObject(jobj);

            // Act
            var requestIdKey = _builder.OcelotJProperty<string>("RequestIdKey", "/api/users");

            // Assert
            Assert.Equal("OcelotRequestId", requestIdKey);
        }

        [Fact]
        public void OcelotJProperty_RoutePropertyIsNull_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Timeout"": null }
              ],
              ""GlobalConfiguration"": {
                ""Timeout"": 30000
              }
            }");
            GivenJObject(jobj);

            // Act
            var timeout = _builder.OcelotJProperty<int>("Timeout", "/api/users");

            // Assert
            Assert.Equal(30000, timeout);
        }

        [Fact]
        public void OcelotJProperty_NestedPropertyPathInRoute_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                {
                  ""UpstreamPathTemplate"": ""/api/users"",
                  ""AuthenticationOptions"": {
                    ""AuthenticationProviderKey"": ""Bearer""
                  }
                }
              ],
              ""GlobalConfiguration"": {}
            }");
            GivenJObject(jobj);

            // Act
            var authKey = _builder.OcelotJProperty<string>("AuthenticationOptions.AuthenticationProviderKey", "/api/users");

            // Assert
            Assert.Equal("Bearer", authKey);
        }

        [Fact]
        public void OcelotJProperty_NestedPropertyPathInGlobal_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"" }
              ],
              ""GlobalConfiguration"": {
                ""ServiceDiscoveryProvider"": {
                  ""Host"": ""localhost""
                }
              }
            }");
            GivenJObject(jobj);

            // Act
            var host = _builder.OcelotJProperty<string>("ServiceDiscoveryProvider.Host", "/api/users");

            // Assert
            Assert.Equal("localhost", host);
        }

        [Fact]
        public void OcelotJProperty_StringProperty_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""DownstreamScheme"": ""https"" }
              ],
              ""GlobalConfiguration"": {}
            }");
            GivenJObject(jobj);

            // Act
            var scheme = _builder.OcelotJProperty<string>("DownstreamScheme", "/api/users");

            // Assert
            Assert.Equal("https", scheme);
        }

        [Fact]
        public void OcelotJProperty_IntegerProperty_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Priority"": 1 }
              ],
              ""GlobalConfiguration"": {
                ""Priority"": 99
              }
            }");
            GivenJObject(jobj);

            // Act
            var priority = _builder.OcelotJProperty<int>("Priority", "/api/users");

            // Assert
            Assert.Equal(1, priority);
        }

        [Fact]
        public void OcelotJProperty_BooleanProperty_ReturnsValue()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""AuthenticationOptions"": { ""Enabled"": true } }
              ],
              ""GlobalConfiguration"": {}
            }");
            GivenJObject(jobj);

            // Act
            var enabled = _builder.OcelotJProperty<bool>("AuthenticationOptions.Enabled", "/api/users");

            // Assert
            Assert.True(enabled);
        }

        [Fact]
        public void OcelotJProperty_RouteNotFound_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""GetUsers"" }
              ],
              ""GlobalConfiguration"": {
                ""BaseUrl"": ""https://ocelot.net""
              }
            }");
            GivenJObject(jobj);

            // Act - route doesn't match, so should return null and then use global
            var baseUrl = _builder.OcelotJProperty<string>("BaseUrl", "/api/nonexistent");

            // Assert
            Assert.Equal("https://ocelot.net", baseUrl);
        }

        [Fact]
        public void OcelotJProperty_FindRouteByKey_ReturnsRouteProperty()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Key"": ""GetUsers"", ""TimeoutInMilliseconds"": 5000 }
              ],
              ""GlobalConfiguration"": {}
            }");
            GivenJObject(jobj);

            // Act - search by key instead of upstream path
            var timeout = _builder.OcelotJProperty<int>("TimeoutInMilliseconds", key: "GetUsers");

            // Assert
            Assert.Equal(5000, timeout);
        }

        [Fact]
        public void OcelotJProperty_CaseSensitiveComparison_FindsExactMatch()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/API/Users"", ""TimeoutInMilliseconds"": 5000 }
              ],
              ""GlobalConfiguration"": {
                ""TimeoutInMilliseconds"": 30000
              }
            }");
            GivenJObject(jobj);

            // Act
            var timeout = _builder.OcelotJProperty<int>("TimeoutInMilliseconds", "/API/Users", comparison: StringComparison.Ordinal);

            // Assert
            Assert.Equal(5000, timeout);
        }

        [Fact]
        public void OcelotJProperty_CaseInsensitiveComparison_IgnoresCase()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""TimeoutInMilliseconds"": 5000 }
              ],
              ""GlobalConfiguration"": {
                ""TimeoutInMilliseconds"": 30000
              }
            }");
            GivenJObject(jobj);

            // Act
            var timeout = _builder.OcelotJProperty<int>("TimeoutInMilliseconds", "/API/USERS", comparison: StringComparison.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(5000, timeout);
        }

        [Fact]
        public void OcelotJProperty_PropertyNullValue_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Description"": null }
              ],
              ""GlobalConfiguration"": {
                ""Description"": ""Default Description""
              }
            }");
            GivenJObject(jobj);

            // Act
            var description = _builder.OcelotJProperty<string>("Description", "/api/users");

            // Assert
            Assert.Equal("Default Description", description);
        }

        [Fact]
        public void OcelotJProperty_GlobalPropertyNotFound_ReturnsDefault()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"" }
              ],
              ""GlobalConfiguration"": {}
            }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJProperty<string>("NonexistentProperty", "/api/users");

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_BothRoutesAndGlobalNull_ReturnsNull()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""Description"": null }
              ],
              ""GlobalConfiguration"": {
                ""Description"": null
              }
            }");
            GivenJObject(jobj);

            // Act
            var actual = _builder.OcelotJProperty<string>("Description", "/api/users");

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_NoRoutes_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [],
              ""GlobalConfiguration"": {
                ""BaseUrl"": ""https://ocelot.net""
              }
            }");
            GivenJObject(jobj);

            // Act
            var baseUrl = _builder.OcelotJProperty<string>("BaseUrl", "/api/nonexistent");

            // Assert
            Assert.Equal("https://ocelot.net", baseUrl);
        }

        [Fact]
        public void OcelotJProperty_MultipleRoutes_FindsCorrectRoute()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [
                { ""UpstreamPathTemplate"": ""/api/users"", ""TimeoutInMilliseconds"": 5000 },
                { ""UpstreamPathTemplate"": ""/api/orders"", ""TimeoutInMilliseconds"": 10000 }
              ],
              ""GlobalConfiguration"": {
                ""TimeoutInMilliseconds"": 30000
              }
            }");
            GivenJObject(jobj);

            // Act
            var timeout = _builder.OcelotJProperty<int>("TimeoutInMilliseconds", "/api/orders");

            // Assert
            Assert.Equal(10000, timeout);
        }

        [Fact]
        public void OcelotJProperty_NullPathTemplate_FallsBackToGlobal()
        {
            // Arrange
            var jobj = JObject.Parse(@"
            {
              ""Routes"": [],
              ""GlobalConfiguration"": {
                ""BaseUrl"": ""https://ocelot.net""
              }
            }");
            GivenJObject(jobj);

            // Act
            var baseUrl = _builder.OcelotJProperty<string>("BaseUrl", null);

            // Assert
            Assert.Equal("https://ocelot.net", baseUrl);
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotJProperty_JObject : UnitTest
    {
        [Fact]
        public void OcelotJProperty_RoutePropertyExists_ReturnsRouteProperty()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""UpstreamPathTemplate"": ""/api/users"",
              ""TimeoutInMilliseconds"": 5000
            }");
            var global = JObject.Parse(@"{ ""TimeoutInMilliseconds"": 30000 }");

            // Act
            var timeout = route.OcelotJProperty<int>("TimeoutInMilliseconds", global);

            // Assert
            Assert.Equal(5000, timeout);
        }

        [Fact]
        public void OcelotJProperty_PropertyNotInRoute_FallsBackToGlobal()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""UpstreamPathTemplate"": ""/api/users"" }");
            var global = JObject.Parse(@"{ ""RequestIdKey"": ""OcelotRequestId"" }");

            // Act
            var requestIdKey = route.OcelotJProperty<string>("RequestIdKey", global);

            // Assert
            Assert.Equal("OcelotRequestId", requestIdKey);
        }

        [Fact]
        public void OcelotJProperty_RoutePropertyIsNull_FallsBackToGlobal()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""UpstreamPathTemplate"": ""/api/users"", ""Timeout"": null }");
            var global = JObject.Parse(@"{ ""Timeout"": 30000 }");

            // Act
            var timeout = route.OcelotJProperty<int>("Timeout", global);

            // Assert
            Assert.Equal(30000, timeout);
        }

        [Fact]
        public void OcelotJProperty_NestedPathInRoute_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""AuthenticationOptions"": {
                ""AuthenticationProviderKey"": ""Bearer""
              }
            }");
            var global = JObject.Parse(@"{ ""BaseUrl"": ""https://ocelot.net"" }");

            // Act
            var authKey = route.OcelotJProperty<string>("AuthenticationOptions.AuthenticationProviderKey", global);

            // Assert
            Assert.Equal("Bearer", authKey);
        }

        [Fact]
        public void OcelotJProperty_NestedPathInGlobal_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""UpstreamPathTemplate"": ""/api/users"" }");
            var global = JObject.Parse(@"
            {
              ""ServiceDiscoveryProvider"": {
                ""Host"": ""localhost""
              }
            }");

            // Act
            var host = route.OcelotJProperty<string>("ServiceDiscoveryProvider.Host", global);

            // Assert
            Assert.Equal("localhost", host);
        }

        [Fact]
        public void OcelotJProperty_StringProperty_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""DownstreamScheme"": ""https"" }");
            var global = JObject.Parse(@"{ ""DownstreamScheme"": ""http"" }");

            // Act
            var scheme = route.OcelotJProperty<string>("DownstreamScheme", global);

            // Assert
            Assert.Equal("https", scheme);
        }

        [Fact]
        public void OcelotJProperty_IntegerProperty_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""Priority"": 1 }");
            var global = JObject.Parse(@"{ ""Priority"": 99 }");

            // Act
            var priority = route.OcelotJProperty<int>("Priority", global);

            // Assert
            Assert.Equal(1, priority);
        }

        [Fact]
        public void OcelotJProperty_BooleanProperty_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""AuthenticationOptions"": { ""Enabled"": true } }");
            var global = JObject.Parse(@"{ ""AuthenticationOptions"": { ""Enabled"": false } }");

            // Act
            var enabled = route.OcelotJProperty<bool>("AuthenticationOptions.Enabled", global);

            // Assert
            Assert.True(enabled);
        }

        [Fact]
        public void OcelotJProperty_NullRoute_FallsBackToGlobal()
        {
            // Arrange
            JObject route = null;
            var global = JObject.Parse(@"{ ""BaseUrl"": ""https://ocelot.net"" }");

            // Act
            var baseUrl = route.OcelotJProperty<string>("BaseUrl", global);

            // Assert
            Assert.Equal("https://ocelot.net", baseUrl);
        }

        [Fact]
        public void OcelotJProperty_NullGlobalConfiguration_ReturnsNull()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""UpstreamPathTemplate"": ""/api/users"" }");
            JObject global = null;

            // Act
            var actual = route.OcelotJProperty<string>("NonexistentProperty", global);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_BothNulls_ReturnsNull()
        {
            // Arrange
            JObject route = null;
            JObject global = null;

            // Act
            var actual = route.OcelotJProperty<string>("AnyProperty", global);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_GlobalPropertyNotFound_ReturnsNull()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""UpstreamPathTemplate"": ""/api/users"" }");
            var global = JObject.Parse(@"{ ""BaseUrl"": ""https://ocelot.net"" }");

            // Act
            var actual = route.OcelotJProperty<string>("NonexistentProperty", global);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_RoutePropertyIsJTokenNull_FallsBackToGlobal()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""Description"": null }");
            var global = JObject.Parse(@"{ ""Description"": ""Global Description"" }");

            // Act
            var description = route.OcelotJProperty<string>("Description", global);

            // Assert
            Assert.Equal("Global Description", description);
        }

        [Fact]
        public void OcelotJProperty_DefaultGlobalConfiguration_UsesStaticMergedConfigJObject()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""Key"": ""route1"" }");
            // Not passing global configuration, so it should use the static MergedConfigJObject if available

            // Act - this tests the null coalescing for globalConfiguration
            var actual = route.OcelotJProperty<string>("NonexistentProperty");

            // Assert - behavior depends on MergedConfigJObject state, but should not throw
            /*Assert.IsType<string>(actual) ||*/ Assert.Null(actual);
        }

        [Fact]
        public void OcelotJProperty_MultipleNestingLevels_ReturnsValue()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""Level1"": {
                ""Level2"": {
                  ""Level3"": {
                    ""Value"": ""NestedValue""
                  }
                }
              }
            }");
            var global = JObject.Parse(@"{}");

            // Act
            var value = route.OcelotJProperty<string>("Level1.Level2.Level3.Value", global);

            // Assert
            Assert.Equal("NestedValue", value);
        }

        [Fact]
        public void OcelotJProperty_ComplexType_ReturnsCorrectly()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""DownstreamHostAndPorts"": [
                { ""Host"": ""localhost"", ""Port"": 5000 }
              ]
            }");
            var global = JObject.Parse(@"{}");

            // Act
            var hostAndPorts = route.OcelotJProperty<JArray>("DownstreamHostAndPorts", global);

            // Assert
            Assert.NotNull(hostAndPorts);
            Assert.Single(hostAndPorts);
        }

        [Fact]
        public void OcelotJProperty_BothRouteAndGlobalHaveProperty_PreferRoute()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""Level1"": {
                ""Level2"": {
                  ""Value"": ""RouteValue""
                }
              }
            }");
            var global = JObject.Parse(@"
            {
              ""Level1"": {
                ""Level2"": {
                  ""Value"": ""GlobalValue""
                }
              }
            }");

            // Act
            var value = route.OcelotJProperty<string>("Level1.Level2.Value", global);

            // Assert - route property should take precedence
            Assert.Equal("RouteValue", value);
        }

        [Fact]
        public void OcelotJProperty_RouteHasPartialPath_FallsBackToGlobal()
        {
            // Arrange
            var route = JObject.Parse(@"
            {
              ""Level1"": {
                ""Level2"": null
              }
            }");
            var global = JObject.Parse(@"
            {
              ""Level1"": {
                ""Level2"": {
                  ""Value"": ""GlobalValue""
                }
              }
            }");

            // Act - path doesn't exist in route, should get null and return global's result
            var value = route.OcelotJProperty<string>("Level1.Level2.Value", global);

            // Assert
            Assert.Equal("GlobalValue", value);
        }

        [Fact]
        public void OcelotJProperty_BothAreNull_ReturnsNull()
        {
            // Arrange
            var route = JObject.Parse(@"{ ""Key"": null }");
            var global = JObject.Parse(@"{ ""Key"": null }");

            // Act
            var actual = route.OcelotJProperty<string>("Key", global);

            // Assert
            Assert.Null(actual);
        }
    }
}

public class ConfigurationBuilderExtensionsTestsBase : FileUnitTest
{
    protected readonly Mock<IWebHostEnvironment> _hostingEnvironment = new();
    protected FileConfiguration _globalConfig;
    protected FileConfiguration _routeA;
    protected FileConfiguration _routeB;
    protected FileConfiguration _aggregate;
    protected FileConfiguration _envSpecific;

    protected void GivenTheEnvironmentIs(string folder, [CallerMemberName] string testName = null)
    {
        _hostingEnvironment.SetupGet(x => x.EnvironmentName).Returns(testName);
        environmentConfigFileName = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, testName));
        files.Add(environmentConfigFileName);
    }
    protected string CreateConfigFile(string folder, string key, FileConfiguration configuration, string fileName = null)
    {
        fileName ??= string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, key);
        var fullPath = Path.Combine(folder, fileName);
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        files.Add(fullPath); // important for cleaning up files
        return fullPath;
    }
    protected void GivenMultipleConfigurationFiles(string folder, bool withEnvironment = false, [CallerMemberName] string testName = null)
    {
        _globalConfig = new() { GlobalConfiguration = GetFileGlobalConfigurationData(testName) };
        _routeA = new() { Routes = GetServiceARoutes() };
        _routeB = new() { Routes = GetServiceBRoutes() };
        _aggregate = new() { Aggregates = GetFileAggregatesRouteData() };
        _envSpecific = new() { Routes = GetEnvironmentSpecificRoutes() };
        var configParts = new Dictionary<string, FileConfiguration>
        {
            { "global", _globalConfig },
            { "routesA", _routeA },
            { "routesB", _routeB },
            { "aggregates", _aggregate },
        };
        if (withEnvironment)
        {
            var envName = _hostingEnvironment?.Object?.EnvironmentName ?? string.Empty;
            configParts.Add(envName, _envSpecific);
        }
        foreach (var part in configParts)
        {
            _ = CreateConfigFile(folder, part.Key, part.Value);
        }
    }
    protected static FileGlobalConfiguration GetFileGlobalConfigurationData(string requestIdKey = null) => new()
    {
        BaseUrl = "BaseUrl",
        RateLimitOptions = new()
        {
            StatusCode = 500,
            ClientIdHeader = "ClientIdHeader",
            QuotaMessage = "QuotaExceededMessage",
            KeyPrefix = "RateLimitCounterPrefix",
        },
        ServiceDiscoveryProvider = new()
        {
            Scheme = "https",
            Host = "Host",
            Port = 80,
            Type = "Type",
        },
        RequestIdKey = requestIdKey ?? "RequestIdKey",
    };
    protected static List<FileRoute> GetServiceARoutes() => [GetRoute("A")];
    protected static List<FileRoute> GetServiceBRoutes() => [GetRoute("B"), GetRoute("BB")];
    protected static List<FileRoute> GetEnvironmentSpecificRoutes() => [GetRoute("Env")];
    protected static FileRoute GetRoute(string name) => new()
    {
        DownstreamScheme = name,
        DownstreamPathTemplate = name,
        Key = name,
        UpstreamHost = name,
        UpstreamHttpMethod = [name],
        DownstreamHostAndPorts = [new(name, 80)],
    };
    protected static List<FileAggregateRoute> GetFileAggregatesRouteData() =>
    [
        new()
        {
            RouteKeys = ["KeyB", "KeyBB"],
            UpstreamPathTemplate = "UpstreamPathTemplate",
        },
    ];
    protected static FileConfiguration GivenCombinedFileConfigurationObject([CallerMemberName] string testName = null)
        => new()
        {
            GlobalConfiguration = GetFileGlobalConfigurationData(testName),
            Routes = GetServiceARoutes().Concat(GetServiceBRoutes()).Concat(GetEnvironmentSpecificRoutes()).ToList(),
            Aggregates = GetFileAggregatesRouteData(),
        };

    protected void TheOcelotPrimaryConfigFileExists(bool expected = true)
        => Assert.Equal(expected, File.Exists(primaryConfigFileName));
}

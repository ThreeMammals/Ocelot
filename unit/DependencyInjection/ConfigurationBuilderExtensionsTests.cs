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

        // Act
        var json = ConfigurationBuilderExtensions.GetMergedOcelotJson(TestID, _hostingEnvironment.Object,
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

        // Act
        var json = ConfigurationBuilderExtensions.GetMergedOcelotJson(TestID, _hostingEnvironment.Object, fileConfiguration: configuration);

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
    public void OcelotMergeConfigurationSection_WhenSourceSectionIsNeitherJObjectNorJArray_DoesNothingAndReturnsTargetUnchanged()
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
        var result = target.OcelotMergeConfigurationSection(source, "Routes");
        result = result.OcelotMergeConfigurationSection(source, "GlobalConfiguration");

        // Assert
        Assert.Same(target, result); // Should return the original target (fluent)

        // Routes section should remain unchanged
        var routesAfter = target.OcelotGetSection("Routes") as JArray;
        Assert.NotNull(routesAfter);
        Assert.Single(routesAfter);
        Assert.Equal("route1", routesAfter[0]["Key"].Value<string>());

        // GlobalConfiguration section should remain unchanged
        var globalAfter = target.OcelotGetSection("GlobalConfiguration") as JObject;
        Assert.NotNull(globalAfter);
        Assert.Equal("https://ocelot.net", globalAfter["BaseUrl"].Value<string>());
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotGetSection : UnitTest
    {
        [Fact]
        public void OcelotGetSection_AllLogicalBranches()
        {
            const string myProp = "MyProp";

            // Scenario 1: obj var is null after casting to JObject
            string json = null;
            JToken token = null; // JToken.Parse(jsonInput);
            var result = token.OcelotGetSection(myProp);
            Assert.Null(result);

            // Scenario 2: obj not null but property not found
            json = "{}";
            token = JToken.Parse(json);
            result = token.OcelotGetSection(myProp);
            Assert.Null(result);

            // Scenario 3: property found but value is null
            json = @"{""MyProp"": null}";
            token = JToken.Parse(json);
            result = token.OcelotGetSection(myProp);
            Assert.NotNull(result);
            Assert.Equal(null, result.Value<int?>());

            // Scenario 4: property found with integer value
            json = @"{""MyProp"": 123}";
            token = JToken.Parse(json);
            result = token.OcelotGetSection(myProp);
            Assert.NotNull(result);
            Assert.Equal(123, result.Value<int>());
        }

        [Fact]
        public void OcelotGetSection_CaseInsensitiveLookup_WorksCorrectly()
        {
            // Arrange
            var token = JObject.Parse(@"{ ""routes"": [1,2,3], ""GLOBALCONFIGURATION"": { ""BaseUrl"": ""https://ocelot.net"" } }");

            // Act & Assert
            Assert.NotNull(token.OcelotGetSection("Routes"));
            Assert.NotNull(token.OcelotGetSection("routes"));
            Assert.NotNull(token.OcelotGetSection("ROUTES"));
            Assert.NotNull(token.OcelotGetSection("GlobalConfiguration"));
            Assert.NotNull(token.OcelotGetSection("globalconfiguration"));
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotSetSection : UnitTest
    {
        [Fact]
        public void OcelotSetSection_NullTarget_DoesNothing()
        {
            JToken to = null;

            // Act
            to.OcelotSetSection(TestName(), JValue.CreateString(TestID));

            Assert.Null(to); // Still null, no exception
        }

        [Fact]
        public void OcelotSetSection_NonJObjectTarget_DoesNothing()
        {
            var array = new JArray { 1, 2, 3 };

            // Act
            array.OcelotSetSection(TestName(), JValue.CreateString(TestID));

            Assert.Equal(3, array.Count);
            Assert.Contains(1, array);
            Assert.Contains(2, array);
            Assert.Contains(3, array);
            var jtoken = array.OcelotGetSection(TestName());
            Assert.Null(jtoken);
        }

        [Theory]
        [InlineData("section")]
        [InlineData("SECTION")]
        [InlineData("SeCtIoN")]
        public void OcelotSetSection_ExistingProperty_UpdatesValue_CaseInsensitive(string propertyName)
        {
            var jObj = JObject.Parse("""{ "section": "oldValue", "other": 42 }""");

            // Act
            jObj.OcelotSetSection(propertyName, JValue.CreateString(TestID));

            Assert.Equal(TestID, (string)jObj["section"]);
            Assert.Equal(42, (int)jObj["other"]); // Other properties unchanged
        }

        [Fact]
        public void OcelotSetSection_NewProperty_AddsIt()
        {
            var jObj = new JObject();
            var newSection = TestName();

            // Act
            jObj.OcelotSetSection(newSection, JValue.CreateString(TestID));

            Assert.Equal(TestID, (string)jObj[newSection]);
            Assert.Single(jObj.Properties());
        }

        [Fact]
        public void OcelotSetSection_UpdatesComplexValue()
        {
            var jObj = JObject.Parse("""{ "config": { "enabled": false } }""");
            var newValue = JObject.Parse("""{ "enabled": true, "timeout": 30 }""");

            // Act
            jObj.OcelotSetSection("config", newValue);

            Assert.True((bool)jObj["config"]["enabled"]!);
            Assert.Equal(30, (int)jObj["config"]["timeout"]!);
        }

        [Fact]
        public void OcelotSetSection_NullValue_SetsNull()
        {
            var jObj = JObject.Parse("""{ "data": "something" }""");

            // Act
            jObj.OcelotSetSection("data", null);

            var actual = jObj["data"];
            Assert.NotNull(actual);
            Assert.True(((JValue)actual).Type == JTokenType.Null);
            Assert.Null(actual.Value<string>());
        }

        [Fact]
        public void OcelotSetSection_EmptyName_AddsProperty()
        {
            var jObj = new JObject();

            // Act
            jObj.OcelotSetSection("", JValue.CreateString("emptyNameValue"));

            Assert.Equal("emptyNameValue", (string)jObj[""]);
        }

        [Fact]
        public void OcelotSetSection_NullName_ThrowsOrHandlesGracefully()
        {
            var jObj = new JObject();

            // Newtonsoft.Json behavior: Add with null name throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
                jObj.OcelotSetSection(null, JValue.CreateString("value")));
        }

        [Theory]
        [InlineData("value", JTokenType.String)]
        [InlineData(123, JTokenType.Integer)]
        [InlineData(true, JTokenType.Boolean)]
        [InlineData(null, JTokenType.Null)]
        public void OcelotSetSection_AcceptsVariousValueTypes(object rawValue, JTokenType expectedType)
        {
            var jObj = new JObject();
            JToken value = rawValue is null ? null : JToken.FromObject(rawValue);

            // Act
            jObj.OcelotSetSection(TestName(), value);

            var actual = jObj[TestName()];
            Assert.NotNull(actual);
            Assert.Equal(expectedType, actual.Type);
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
    protected static List<FileRoute> GetServiceARoutes() => new() { GetRoute("A") };
    protected static List<FileRoute> GetServiceBRoutes() => new() { GetRoute("B"), GetRoute("BB") };
    protected static List<FileRoute> GetEnvironmentSpecificRoutes() => new() { GetRoute("Env") };
    protected static FileRoute GetRoute(string name) => new()
    {
        DownstreamScheme = name,
        DownstreamPathTemplate = name,
        Key = name,
        UpstreamHost = name,
        UpstreamHttpMethod = [name],
        DownstreamHostAndPorts = [new(name, 80)],
    };
    protected static List<FileAggregateRoute> GetFileAggregatesRouteData() => new()
    {
        new()
        {
            RouteKeys = ["KeyB", "KeyBB"],
            UpstreamPathTemplate = "UpstreamPathTemplate",
        },
    };
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

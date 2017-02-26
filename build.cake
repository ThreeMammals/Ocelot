#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=GitReleaseNotes"
#addin "nuget:?package=Cake.DoInDirectory"
#addin "nuget:?package=Cake.Json"

// compile
var compileConfig = Argument("configuration", "Release");
var projectJson = "./src/Ocelot/project.json";

// build artifacts
var artifactsDir = Directory("artifacts");

// unit testing
var artifactsForUnitTestsDir = artifactsDir + Directory("UnitTests");
var unitTestAssemblies = @"./test/Ocelot.UnitTests";

// acceptance testing
var artifactsForAcceptanceTestsDir = artifactsDir + Directory("AcceptanceTests");

// integration testing
var artifactsForIntegrationTestsDir = artifactsDir + Directory("IntegrationTests");

// benchmark testing
var artifactsForBenchmarkTestsDir = artifactsDir + Directory("BenchmarkTests");
var benchmarkTestAssemblies = @"./test/Ocelot.Benchmarks";

// packaging
var packagesDir = artifactsDir + Directory("Packages");
var releaseNotesFile = packagesDir + File("releasenotes.md");
var artifactsFile = packagesDir + File("artifacts.txt");

// unstable releases
var nugetFeedUnstableKey = EnvironmentVariable("nuget-apikey-unstable");
var nugetFeedUnstableUploadUrl = "https://www.nuget.org/api/v2/package";
var nugetFeedUnstableSymbolsUploadUrl = "https://www.nuget.org/api/v2/package";

// stable releases
var tagsUrl = "https://api.github.com/repos/tompallister/ocelot/releases/tags/";
var nugetFeedStableKey = EnvironmentVariable("nuget-apikey-stable");
var nugetFeedStableUploadUrl = "https://www.nuget.org/api/v2/package";
var nugetFeedStableSymbolsUploadUrl = "https://www.nuget.org/api/v2/package";

// internal build variables - don't change these.
var releaseTag = "";
string committedVersion = "0.0.0-dev";
var buildVersion = committedVersion;
GitVersion versioning = null;
var nugetFeedUnstableBranchFilter = "^(develop)$|^(PullRequest/)";

var target = Argument("target", "Default");


Information("target is " +target);
Information("Build configuration is " + compileConfig);	

Task("Default")
	.IsDependentOn("Build");

Task("Build")
	.IsDependentOn("RunTests")
	.IsDependentOn("CreatePackages");

Task("BuildAndReleaseUnstable")
	.IsDependentOn("Build")
	.IsDependentOn("ReleasePackagesToUnstableFeed");
	
Task("Clean")
	.Does(() =>
	{
        if (DirectoryExists(artifactsDir))
        {
            DeleteDirectory(artifactsDir, recursive:true);
        }
        CreateDirectory(artifactsDir);
	});
	
Task("Version")
	.Does(() =>
	{
		versioning = GetNuGetVersionForCommit();
		var nugetVersion = versioning.NuGetVersion;

		Information("SemVer version number: " + nugetVersion);

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			Information("Persisting version number...");
			PersistVersion(committedVersion, nugetVersion);
			buildVersion = nugetVersion;
		}
		else
		{
			Information("We are not running on build server, so we won't persist the version number.");
		}
	});

Task("Restore")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.Does(() =>
	{	
		DotNetCoreRestore("./src");
		DotNetCoreRestore("./test");
	});

Task("RunUnitTests")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		var buildSettings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
		};

		EnsureDirectoryExists(artifactsForUnitTestsDir);
		DotNetCoreTest(unitTestAssemblies, buildSettings);
	});

Task("RunAcceptanceTests")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		var buildSettings = new DotNetCoreTestSettings
		{
			Configuration = "Debug", //acceptance test config is hard-coded for debug
		};

		EnsureDirectoryExists(artifactsForAcceptanceTestsDir);

		DoInDirectory("test/Ocelot.AcceptanceTests", () =>
		{
			DotNetCoreTest(".", buildSettings);
		});

	});

	Task("RunIntegrationTests")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		var buildSettings = new DotNetCoreTestSettings
		{
			Configuration = "Debug", //int test config is hard-coded for debug
		};

		EnsureDirectoryExists(artifactsForIntegrationTestsDir);

		DoInDirectory("test/Ocelot.IntegrationTests", () =>
		{
			DotNetCoreTest(".", buildSettings);
		});

	});

Task("RunBenchmarkTests")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		var buildSettings = new DotNetCoreRunSettings
		{
			Configuration = compileConfig,
		};

		EnsureDirectoryExists(artifactsForBenchmarkTestsDir);

		DoInDirectory(benchmarkTestAssemblies, () =>
		{
			DotNetCoreRun(".", "", buildSettings);
		});
	});

Task("RunTests")
	.IsDependentOn("RunUnitTests")
	.IsDependentOn("RunAcceptanceTests")
	.IsDependentOn("RunIntegrationTests")
	.Does(() =>
	{
	});

Task("CreatePackages")
	.Does(() => 
	{
		EnsureDirectoryExists(packagesDir);
        
		GenerateReleaseNotes(releaseNotesFile);

		var settings = new DotNetCorePackSettings
			{
				OutputDirectory = packagesDir,
				NoBuild = true
			};

		DotNetCorePack(projectJson, settings);

        System.IO.File.WriteAllLines(artifactsFile, new[]{
            "nuget:Ocelot." + buildVersion + ".nupkg",
            "nugetSymbols:Ocelot." + buildVersion + ".symbols.nupkg",
            "releaseNotes:releasenotes.md"
        });

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			var path = packagesDir.ToString() + @"/**/*";

			foreach (var file in GetFiles(path))
			{
				AppVeyor.UploadArtifact(file.FullPath);
			}
		}
	});

Task("ReleasePackagesToUnstableFeed")
	.IsDependentOn("CreatePackages")
	.Does(() =>
	{
		if (ShouldPublishToUnstableFeed(nugetFeedUnstableBranchFilter, versioning.BranchName))
		{		
			PublishPackages(packagesDir, artifactsFile, nugetFeedUnstableKey, nugetFeedUnstableUploadUrl, nugetFeedUnstableSymbolsUploadUrl);
		}
	});

Task("EnsureStableReleaseRequirements")
    .Does(() =>
    {
        if (!AppVeyor.IsRunningOnAppVeyor)
		{
           throw new Exception("Stable release should happen via appveyor");
		}
        
		var isTag =
           AppVeyor.Environment.Repository.Tag.IsTag &&
           !string.IsNullOrWhiteSpace(AppVeyor.Environment.Repository.Tag.Name);

        if (!isTag)
		{
           throw new Exception("Stable release should happen from a published GitHub release");
		}
    });

Task("UpdateVersionInfo")
    .IsDependentOn("EnsureStableReleaseRequirements")
    .Does(() =>
    {
        releaseTag = AppVeyor.Environment.Repository.Tag.Name;
        AppVeyor.UpdateBuildVersion(releaseTag);
    });

Task("DownloadGitHubReleaseArtifacts")
    .IsDependentOn("UpdateVersionInfo")
    .Does(() =>
    {
        EnsureDirectoryExists(packagesDir);

		var releaseUrl = tagsUrl + releaseTag;
        var assets_url = ParseJson(GetResource(releaseUrl))
            .GetValue("assets_url")
			.Value<string>();

        foreach(var asset in DeserializeJson<JArray>(GetResource(assets_url)))
        {
			var file = packagesDir + File(asset.Value<string>("name"));
			Information("Downloading " + file);
            DownloadFile(asset.Value<string>("browser_download_url"), file);
        }
    });

Task("ReleasePackagesToStableFeed")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
    {
		PublishPackages(packagesDir, artifactsFile, nugetFeedStableKey, nugetFeedStableUploadUrl, nugetFeedStableSymbolsUploadUrl);
    });

Task("Release")
    .IsDependentOn("ReleasePackagesToStableFeed");

RunTarget(target);

/// Gets nuique nuget version for this commit
private GitVersion GetNuGetVersionForCommit()
{
    GitVersion(new GitVersionSettings{
        UpdateAssemblyInfo = false,
        OutputType = GitVersionOutput.BuildServer
    });

    return GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
}

/// Updates project version in all of our projects
private void PersistVersion(string committedVersion, string newVersion)
{
	Information(string.Format("We'll search all project.json files for {0} and replace with {1}...", committedVersion, newVersion));

	var projectJsonFiles = GetFiles("./**/project.json");

	foreach(var projectJsonFile in projectJsonFiles)
	{
		var file = projectJsonFile.ToString();
 
		Information(string.Format("Updating {0}...", file));

		var updatedProjectJson = System.IO.File.ReadAllText(file)
			.Replace(committedVersion, newVersion);

		System.IO.File.WriteAllText(file, updatedProjectJson);
	}
}

/// generates release notes based on issues closed in GitHub since the last release
private void GenerateReleaseNotes(ConvertableFilePath file)
{
	if (!IsRunningOnWindows())
	{
		Warning("We can't generate release notes as we're not running on Windows.");
		return;
	}

	Information("Generating release notes at " + file);

    var releaseNotesExitCode = StartProcess(
        @"tools/GitReleaseNotes/tools/gitreleasenotes.exe", 
        new ProcessSettings { Arguments = ". /o " + file });

    if (string.IsNullOrEmpty(System.IO.File.ReadAllText(file)))
	{
        System.IO.File.WriteAllText(file, "No issues closed since last release");
	}

    if (releaseNotesExitCode != 0) 
	{
		throw new Exception("Failed to generate release notes");
	}
}

/// Publishes code and symbols packages to nuget feed, based on contents of artifacts file
private void PublishPackages(ConvertableDirectoryPath packagesDir, ConvertableFilePath artifactsFile, string feedApiKey, string codeFeedUrl, string symbolFeedUrl)
{
        var artifacts = System.IO.File
            .ReadAllLines(artifactsFile)
            .Select(l => l.Split(':'))
            .ToDictionary(v => v[0], v => v[1]);

		var codePackage = packagesDir + File(artifacts["nuget"]);

		Information("Pushing package " + codePackage);
        NuGetPush(
            codePackage,
            new NuGetPushSettings {
                ApiKey = feedApiKey,
                Source = codeFeedUrl
            });
}

/// gets the resource from the specified url
private string GetResource(string url)
{
	Information("Getting resource from " + url);

    var assetsRequest = System.Net.WebRequest.CreateHttp(url);
    assetsRequest.Method = "GET";
    assetsRequest.Accept = "application/vnd.github.v3+json";
    assetsRequest.UserAgent = "BuildScript";

    using (var assetsResponse = assetsRequest.GetResponse())
    {
        var assetsStream = assetsResponse.GetResponseStream();
        var assetsReader = new StreamReader(assetsStream);
        return assetsReader.ReadToEnd();
    }
}

private bool ShouldPublishToUnstableFeed(string filter, string branchName)
{
	var regex = new System.Text.RegularExpressions.Regex(filter);
	var publish = regex.IsMatch(branchName);
	if (publish)
	{
		Information("Branch " + branchName + " will be published to the unstable feed");
	}
	else
	{
		Information("Branch " + branchName + " will not be published to the unstable feed");
	}
	return publish;	
}
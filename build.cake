#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=GitReleaseNotes"
#addin nuget:?package=Cake.Json
#addin nuget:?package=Newtonsoft.Json
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=coveralls.net&version=0.7.0"
#addin Cake.Coveralls&version=0.7.0

// compile
var compileConfig = Argument("configuration", "Release");
var slnFile = "./Ocelot.sln";

// build artifacts
var artifactsDir = Directory("artifacts");

// unit testing
var artifactsForUnitTestsDir = artifactsDir + Directory("UnitTests");
var unitTestAssemblies = @"./test/Ocelot.UnitTests/Ocelot.UnitTests.csproj";
var minCodeCoverage = 80d;
var coverallsRepoToken = "coveralls-repo-token-ocelot";
var coverallsRepo = "https://coveralls.io/github/TomPallister/Ocelot";

// acceptance testing
var artifactsForAcceptanceTestsDir = artifactsDir + Directory("AcceptanceTests");
var acceptanceTestAssemblies = @"./test/Ocelot.AcceptanceTests/Ocelot.AcceptanceTests.csproj";

// integration testing
var artifactsForIntegrationTestsDir = artifactsDir + Directory("IntegrationTests");
var integrationTestAssemblies = @"./test/Ocelot.IntegrationTests/Ocelot.IntegrationTests.csproj";

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

Task("Compile")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.Does(() =>
	{	
		var settings = new DotNetCoreBuildSettings
		{
			Configuration = compileConfig,
		};
		
		DotNetCoreBuild(slnFile, settings);
	});

Task("RunUnitTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		if (IsRunningOnWindows())
		{
			var coverageSummaryFile = artifactsForUnitTestsDir + File("coverage.xml");
        
			EnsureDirectoryExists(artifactsForUnitTestsDir);
        
			OpenCover(tool => 
				{
					tool.DotNetCoreTest(unitTestAssemblies);
				},
				new FilePath(coverageSummaryFile),
				new OpenCoverSettings()
				{
					Register="user",
					ArgumentCustomization=args=>args.Append(@"-oldstyle -returntargetcode -excludebyattribute:*.ExcludeFromCoverage*")
				}
				.WithFilter("+[Ocelot*]*")
				.WithFilter("-[xunit*]*")
				.WithFilter("-[Ocelot*Tests]*")
			);
        
			ReportGenerator(coverageSummaryFile, artifactsForUnitTestsDir);
		
			if (AppVeyor.IsRunningOnAppVeyor)
			{
				var repoToken = EnvironmentVariable(coverallsRepoToken);
				if (string.IsNullOrEmpty(repoToken))
				{
					throw new Exception(string.Format("Coveralls repo token not found. Set environment variable '{0}'", coverallsRepoToken));
				}

				Information(string.Format("Uploading test coverage to {0}", coverallsRepo));
				CoverallsNet(coverageSummaryFile, CoverallsNetReportType.OpenCover, new CoverallsNetSettings()
				{
					RepoToken = repoToken
				});
			}
			else
			{
				Information("We are not running on the build server so we won't publish the coverage report to coveralls.io");
			}

			var sequenceCoverage = XmlPeek(coverageSummaryFile, "//CoverageSession/Summary/@sequenceCoverage");
			var branchCoverage = XmlPeek(coverageSummaryFile, "//CoverageSession/Summary/@branchCoverage");

			Information("Sequence Coverage: " + sequenceCoverage);
		
			if(double.Parse(sequenceCoverage) < minCodeCoverage)
			{
				var whereToCheck = !AppVeyor.IsRunningOnAppVeyor ? coverallsRepo : artifactsForUnitTestsDir;
				throw new Exception(string.Format("Code coverage fell below the threshold of {0}%. You can find the code coverage report at {1}", minCodeCoverage, whereToCheck));
			};
		
		}
		else
		{
			var settings = new DotNetCoreTestSettings
			{
				Configuration = compileConfig,
			};

			EnsureDirectoryExists(artifactsForUnitTestsDir);
			DotNetCoreTest(unitTestAssemblies, settings);
		}
	});

Task("RunAcceptanceTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		if(TravisCI.IsRunningOnTravisCI)
		{
			Information(
				@"Job:
				JobId: {0}
				JobNumber: {1}
				OSName: {2}",
				BuildSystem.TravisCI.Environment.Job.JobId,
				BuildSystem.TravisCI.Environment.Job.JobNumber,
				BuildSystem.TravisCI.Environment.Job.OSName
			);

			if(TravisCI.Environment.Job.OSName.ToLower() == "osx")
			{
				return;
			}
		}

		var settings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
			ArgumentCustomization = args => args
				.Append("--no-restore")
				.Append("--no-build")
		};

		EnsureDirectoryExists(artifactsForAcceptanceTestsDir);
		DotNetCoreTest(acceptanceTestAssemblies, settings);
	});

Task("RunIntegrationTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		if(TravisCI.IsRunningOnTravisCI)
		{
			Information(
				@"Job:
				JobId: {0}
				JobNumber: {1}
				OSName: {2}",
				BuildSystem.TravisCI.Environment.Job.JobId,
				BuildSystem.TravisCI.Environment.Job.JobNumber,
				BuildSystem.TravisCI.Environment.Job.OSName
			);

			if(TravisCI.Environment.Job.OSName.ToLower() == "osx")
			{
				return;
			}
		}

		var settings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
			ArgumentCustomization = args => args
				.Append("--no-restore")
				.Append("--no-build")
		};

		EnsureDirectoryExists(artifactsForIntegrationTestsDir);
		DotNetCoreTest(integrationTestAssemblies, settings);
	});

Task("RunTests")
	.IsDependentOn("RunUnitTests")
	.IsDependentOn("RunAcceptanceTests")
	.IsDependentOn("RunIntegrationTests");

Task("CreatePackages")
	.IsDependentOn("Compile")
	.Does(() => 
	{
		EnsureDirectoryExists(packagesDir);

		CopyFiles("./src/**/Release/Ocelot.*.nupkg", packagesDir);

		//GenerateReleaseNotes(releaseNotesFile);

		var projectFiles = GetFiles("./src/**/Release/Ocelot.*.nupkg");

		foreach(var projectFile in projectFiles)
		{
			System.IO.File.AppendAllLines(artifactsFile, new[]{
				projectFile.GetFilename().FullPath,
				//"releaseNotes:releasenotes.md"
			});
		}

		var artifacts = System.IO.File
			.ReadAllLines(artifactsFile)
			.Distinct();
		
		foreach(var artifact in artifacts)
		{
			var codePackage = packagesDir + File(artifact);

			Information("Created package " + codePackage);
		}

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
		Information("Check if stable release...");

        if (!AppVeyor.IsRunningOnAppVeyor)
		{
           throw new Exception("Stable release should happen via appveyor");
		}

		Information("Running on AppVeyor...");

		Information("IsTag = " + AppVeyor.Environment.Repository.Tag.IsTag);

		Information("Name = " + AppVeyor.Environment.Repository.Tag.Name);

		var isTag =
           AppVeyor.Environment.Repository.Tag.IsTag &&
           !string.IsNullOrWhiteSpace(AppVeyor.Environment.Repository.Tag.Name);

        if (!isTag)
		{
           throw new Exception("Stable release should happen from a published GitHub release");
		}

		Information("Release is stable...");
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
		try
		{
			Information("DownloadGitHubReleaseArtifacts");

			EnsureDirectoryExists(packagesDir);

			Information("Directory exists...");

			var releaseUrl = tagsUrl + releaseTag;

			Information("Release url " + releaseUrl);

        	var assets_url = Newtonsoft.Json.Linq.JObject.Parse(GetResource(releaseUrl))
				.Value<string>("assets_url");

			Information("Assets url " + assets_url);

			var assets = GetResource(assets_url);

			Information("Assets " + assets_url);

			foreach(var asset in Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(assets))
			{
				Information("In the loop..");

				var file = packagesDir + File(asset.Value<string>("name"));

				Information("Downloading " + file);
				
				DownloadFile(asset.Value<string>("browser_download_url"), file);
			}

			Information("Out of the loop...");
		}
		catch(Exception exception)
		{
			Information("There was an exception " + exception);
			throw;
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
	Information(string.Format("We'll search all csproj files for {0} and replace with {1}...", committedVersion, newVersion));

	var projectFiles = GetFiles("./**/*.csproj");

	foreach(var projectFile in projectFiles)
	{
		var file = projectFile.ToString();
 
		Information(string.Format("Updating {0}...", file));

		var updatedProjectFile = System.IO.File.ReadAllText(file)
			.Replace(committedVersion, newVersion);

		System.IO.File.WriteAllText(file, updatedProjectFile);
	}
}

/// generates release notes based on issues closed in GitHub since the last release
private void GenerateReleaseNotes(ConvertableFilePath file)
{
	if(!IsRunningOnWindows())
	{
        Warning("We are not running on Windows so we cannot generate release notes.");
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
			.Distinct();
		
		foreach(var artifact in artifacts)
		{
			var codePackage = packagesDir + File(artifact);

			Information("Pushing package " + codePackage);
			
			Information("Calling NuGetPush");

			NuGetPush(
				codePackage,
				new NuGetPushSettings {
					ApiKey = feedApiKey,
					Source = codeFeedUrl
				});
		}
}

/// gets the resource from the specified url
private string GetResource(string url)
{
	try
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
			var response =  assetsReader.ReadToEnd();

			Information("Response is " + response);
			
			return response;
		}
	}
	catch(Exception exception)
	{
		Information("There was an exception " + exception);
		throw;
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

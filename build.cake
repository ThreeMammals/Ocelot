#tool "nuget:?package=GitVersion.CommandLine&version=5.0.1"
#tool "nuget:?package=GitReleaseNotes"
#addin nuget:?package=Cake.Json
#addin nuget:?package=Newtonsoft.Json
#addin nuget:?package=System.Net.Http
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=coveralls.net&version=0.7.0"
#addin Cake.Coveralls&version=0.10.1

// compile
var compileConfig = Argument("configuration", "Release");

var slnFile = "./Ocelot.sln";

// build artifacts
var artifactsDir = Directory("artifacts");

// unit testing
var artifactsForUnitTestsDir = artifactsDir + Directory("UnitTests");
var unitTestAssemblies = @"./test/Ocelot.UnitTests/Ocelot.UnitTests.csproj";
var minCodeCoverage = 80d;
var coverallsRepoToken = "OCELOT_COVERALLS_TOKEN";
var coverallsRepo = "https://coveralls.io/github/ThreeMammals/Ocelot";

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

// stable releases
var tagsUrl = "https://api.github.com/repos/ThreeMammals/ocelot/releases/tags/";
var nugetFeedStableKey = EnvironmentVariable("OCELOT_NUTGET_API_KEY");
var nugetFeedStableUploadUrl = "https://www.nuget.org/api/v2/package";
var nugetFeedStableSymbolsUploadUrl = "https://www.nuget.org/api/v2/package";

// internal build variables - don't change these.
string committedVersion = "0.0.0-dev";
GitVersion versioning = null;
int releaseId = 0;
string gitHubUsername = "TomPallister";
string gitHubPassword = Environment.GetEnvironmentVariable("OCELOT_GITHUB_API_KEY");

var target = Argument("target", "Default");

Information("target is " + target);
Information("Build configuration is " + compileConfig);	

Task("Default")
	.IsDependentOn("Build");

Task("Build")
	.IsDependentOn("RunTests");

Task("RunTests")
	.IsDependentOn("RunUnitTests")
	.IsDependentOn("RunAcceptanceTests")
	.IsDependentOn("RunIntegrationTests");

Task("Release")
	.IsDependentOn("Build")
	.IsDependentOn("CreateArtifacts")
	.IsDependentOn("PublishGitHubRelease")
    .IsDependentOn("PublishToNuget");

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

		if (IsRunningOnCircleCI())
		{
			Information("Persisting version number...");
			PersistVersion(committedVersion, nugetVersion);
		}
		else
		{
			Information("We are not running on build server, so we won't persist the version number.");
		}
	});

Task("RunUnitTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		var testSettings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
			ResultsDirectory = artifactsForUnitTestsDir,
			ArgumentCustomization = args => args
				// this create the code coverage report
				.Append("--settings test/Ocelot.UnitTests/UnitTests.runsettings")
		};

		EnsureDirectoryExists(artifactsForUnitTestsDir);
		DotNetCoreTest(unitTestAssemblies, testSettings);

		var coverageSummaryFile = GetSubDirectories(artifactsForUnitTestsDir).First().CombineWithFilePath(File("coverage.opencover.xml"));
		Information(coverageSummaryFile);
		Information(artifactsForUnitTestsDir);
		// todo bring back report generator to get a friendly report
		// ReportGenerator(coverageSummaryFile, artifactsForUnitTestsDir);
		// https://github.com/danielpalme/ReportGenerator
	
		if (IsRunningOnCircleCI())
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
			var whereToCheck = !IsRunningOnCircleCI() ? coverallsRepo : artifactsForUnitTestsDir;
			throw new Exception(string.Format("Code coverage fell below the threshold of {0}%. You can find the code coverage report at {1}", minCodeCoverage, whereToCheck));
		};
	});

Task("RunAcceptanceTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
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

Task("CreateArtifacts")
	.IsDependentOn("Compile")
	.Does(() => 
	{
		EnsureDirectoryExists(packagesDir);

		CopyFiles("./src/**/Release/Ocelot.*.nupkg", packagesDir);

		// todo fix this for docker build
		//GenerateReleaseNotes(releaseNotesFile);

		var projectFiles = GetFiles("./src/**/Release/Ocelot.*.nupkg");

		foreach(var projectFile in projectFiles)
		{
			System.IO.File.AppendAllLines(artifactsFile, new[]{
				projectFile.GetFilename().FullPath,
				// todo fix this for docker build
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
	});

Task("PublishGitHubRelease")
	.IsDependentOn("CreateArtifacts")
	.Does(() => 
	{
		if (IsRunningOnCircleCI())
		{
			var path = packagesDir.ToString() + @"/**/*";

			CreateGitHubRelease();

			foreach (var file in GetFiles(path))
			{
				UploadFileToGitHubRelease(file);
			}

			CompleteGitHubRelease();
		}
	});

Task("EnsureStableReleaseRequirements")
    .Does(() =>	
    {
		Information("Check if stable release...");

        if (!IsRunningOnCircleCI())
		{
           throw new Exception("Stable release should happen via circleci");
		}

		Information("Release is stable...");
    });

Task("DownloadGitHubReleaseArtifacts")
    .Does(() =>
    {

		try
		{
			EnsureDirectoryExists(packagesDir);

			var releaseUrl = tagsUrl + versioning.NuGetVersion;

        	var assets_url = Newtonsoft.Json.Linq.JObject.Parse(GetResource(releaseUrl))
				.Value<string>("assets_url");

			var assets = GetResource(assets_url);

			foreach(var asset in Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(assets))
			{
				var file = packagesDir + File(asset.Value<string>("name"));
				DownloadFile(asset.Value<string>("browser_download_url"), file);
			}
		}
		catch(Exception exception)
		{
			Information("There was an exception " + exception);
			throw;
		}
    });

Task("PublishToNuget")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
    {
		if (IsRunningOnCircleCI())
		{
			PublishPackages(packagesDir, artifactsFile, nugetFeedStableKey, nugetFeedStableUploadUrl, nugetFeedStableSymbolsUploadUrl);
		}
    });

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
		Information("PublishPackages");
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

private void CreateGitHubRelease()
{
	var json = $"{{ \"tag_name\": \"{versioning.NuGetVersion}\", \"target_commitish\": \"master\", \"name\": \"{versioning.NuGetVersion}\", \"body\": \"todo: notes coming\", \"draft\": true, \"prerelease\": true }}";
	var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

	using(var client = new System.Net.Http.HttpClient())
	{	
			client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(
            System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{gitHubUsername}:{gitHubPassword}")));

		client.DefaultRequestHeaders.Add("User-Agent", "Ocelot Release");

		var result = client.PostAsync("https://api.github.com/repos/ThreeMammals/Ocelot/releases", content).Result;
		if(result.StatusCode != System.Net.HttpStatusCode.Created) 
		{
			throw new Exception("CreateGitHubRelease result.StatusCode = " + result.StatusCode);
		}
		var returnValue = result.Content.ReadAsStringAsync().Result;
		dynamic test = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(returnValue);
		releaseId = test.id;
	}
}

private void UploadFileToGitHubRelease(FilePath file)
{
	var data = System.IO.File.ReadAllBytes(file.FullPath);
	var content = new System.Net.Http.ByteArrayContent(data);
	content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

	using(var client = new System.Net.Http.HttpClient())
	{	
			client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(
            System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{gitHubUsername}:{gitHubPassword}")));

		client.DefaultRequestHeaders.Add("User-Agent", "Ocelot Release");

		var result = client.PostAsync($"https://uploads.github.com/repos/ThreeMammals/Ocelot/releases/{releaseId}/assets?name={file.GetFilename()}", content).Result;
		if(result.StatusCode != System.Net.HttpStatusCode.Created) 
		{
			throw new Exception("UploadFileToGitHubRelease result.StatusCode = " + result.StatusCode);
		}
	}
}

private void CompleteGitHubRelease()
{
	var json = $"{{ \"tag_name\": \"{versioning.NuGetVersion}\", \"target_commitish\": \"master\", \"name\": \"{versioning.NuGetVersion}\", \"body\": \"todo: notes coming\", \"draft\": false, \"prerelease\": false }}";
	var request = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod("Patch"), $"https://api.github.com/repos/ThreeMammals/Ocelot/releases/{releaseId}");
	request.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

	using(var client = new System.Net.Http.HttpClient())
	{	
			client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(
            System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{gitHubUsername}:{gitHubPassword}")));

		client.DefaultRequestHeaders.Add("User-Agent", "Ocelot Release");

		var result = client.SendAsync(request).Result;
		if(result.StatusCode != System.Net.HttpStatusCode.OK) 
		{
			throw new Exception("CompleteGitHubRelease result.StatusCode = " + result.StatusCode);
		}
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

private bool IsRunningOnCircleCI()
{
    return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CIRCLECI"));
}
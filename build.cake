#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=GitReleaseNotes"
#addin nuget:?package=Cake.DoInDirectory

var target = Argument("target", "Default");
var artifactsDir = Directory("artifacts");

Information("target is " +target);

// versioning
var committedVersion = "0.0.0-dev";
var buildVersion = committedVersion;

//compile
var compileConfig = Argument("configuration", "Release");
Information("Build configuration is " + compileConfig);	

// unit testing
var artifactsForUnitTestsDir = artifactsDir + Directory("UnitTests");
var unitTestAssemblies = @"./test/Ocelot.UnitTests";

// acceptance testing
var artifactsForAcceptanceTestsDir = artifactsDir + Directory("AcceptanceTests");
var acceptanceTestAssemblies = @"./test/Ocelot.AcceptanceTests";

//benchmark testing
var artifactsForBenchmarkTestsDir = artifactsDir + Directory("BenchmarkTests");
var benchmarkTestAssemblies = @"./test/Ocelot.Benchmarks";

// packaging
var packagesDir = artifactsDir + Directory("Packages");
var projectJson = "./src/Ocelot/project.json";

// release notes
var releaseNotesFile = packagesDir + File("releasenotes.md");

Task("Default")
	.IsDependentOn("RunTests")
	.IsDependentOn("Package")
	.Does(() =>
	{
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
		var nugetVersion = GetVersion();
		Information("SemVer version number: " + nugetVersion);

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			Information("Persisting version number...");
			PersistVersion(nugetVersion);
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
			DotNetCoreRun(".", "--args", buildSettings);
		});
	});

Task("RunTests")
	.IsDependentOn("RunUnitTests")
	.IsDependentOn("RunAcceptanceTests")
	.Does(() =>
	{
	});

Task("Package")
	.Does(() => 
	{
		EnsureDirectoryExists(packagesDir);
        
		GenerateReleaseNotes();

		var settings = new DotNetCorePackSettings
			{
				OutputDirectory = packagesDir,
				NoBuild = true
			};

		DotNetCorePack(projectJson, settings);

        System.IO.File.WriteAllLines(packagesDir + File("artifacts"), new[]{
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

RunTarget(target);

private string GetVersion()
{
    GitVersion(new GitVersionSettings{
        UpdateAssemblyInfo = false,
        OutputType = GitVersionOutput.BuildServer
    });

    var versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
	return versionInfo.NuGetVersion;
}

private void PersistVersion(string version)
{
	Information(string.Format("We'll search all project.json files for {0} and replace with {1}...", committedVersion, version));
	var projectJsonFiles = GetFiles("./**/project.json");

	foreach(var projectJsonFile in projectJsonFiles)
	{
		var file = projectJsonFile.ToString();
 
		Information(string.Format("Updating {0}...", file));

		var updatedProjectJson = System.IO.File.ReadAllText(file)
			.Replace(committedVersion, version);

		System.IO.File.WriteAllText(file, updatedProjectJson);
	}
}

private void GenerateReleaseNotes()
{
	Information("Generating release notes at " + releaseNotesFile);

    var releaseNotesExitCode = StartProcess(
        @"tools/GitReleaseNotes/tools/gitreleasenotes.exe", 
        new ProcessSettings { Arguments = ". /o " + releaseNotesFile });

    if (string.IsNullOrEmpty(System.IO.File.ReadAllText(releaseNotesFile)))
	{
        System.IO.File.WriteAllText(releaseNotesFile, "No issues closed since last release");
	}

    if (releaseNotesExitCode != 0) 
	{
		throw new Exception("Failed to generate release notes");
	}
}
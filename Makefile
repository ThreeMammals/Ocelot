NAME ?= ocelot

build:
	dotnet tool restore
	dotnet cake

build_and_run_tests:
	dotnet tool restore
	dotnet cake --target=RunTests

release:
	dotnet tool restore		
	dotnet cake --target=Release

run_acceptance_tests:
	dotnet tool restore
	dotnet cake --target=RunAcceptanceTests

run_benchmarks:
	dotnet tool restore
	dotnet cake --target=RunBenchmarkTests

run_unit_tests:
	dotnet tool restore
	dotnet cake --target=RunUnitTests

release_notes:
	dotnet tool restore
	dotnet cake --target=ReleaseNotes

	
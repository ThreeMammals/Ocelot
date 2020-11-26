NAME ?= ocelot

build:
	./build.sh

build_and_run_tests:
	./build.sh --target=RunTests

release:
	./build.sh --target=Release

run_acceptance_tests:
	./build.sh --target=RunAcceptanceTests

run_benchmarks:
	./build.sh --target=RunBenchmarkTests

run_unit_tests:
	./build.sh --target=RunUnitTests

release_notes:
	./build.sh --target=ReleaseNotes
	
# clean the dirs
# version the code and update the csproj files
# write the release notes
# build the code
# unit test
# acceptance test
# integration test
# gather the nuget packages 
# publish release to NuGet
# publish release to GitHub

	
NAME ?= ocelot

build:
	./build.sh

build_and_release_unstable:
	./build.ps1 -target BuildAndReleaseUnstable && exit $LASTEXITCODE

build_and_run_tests:
	./build.ps1 -target RunTests && exit $LASTEXITCODE

release:
	./build.ps1 -target Release && exit $LASTEXITCODE

run_acceptance_tests:
	./build -target RunAcceptanceTests && exit $LASTEXITCODE

run_benchmarks:
	./build.ps1 -target RunBenchmarkTests && exit $LASTEXITCODE

run_unit_tests:
	./build.ps1 -target RunUnitTests && exit $LASTEXITCODE

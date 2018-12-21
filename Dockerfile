FROM microsoft/dotnet:2.1.502-sdk AS builder
WORKDIR /build

#First we add only the project files so that we can cache nuget packages with dotnet restore
COPY Ocelot.sln Ocelot.sln
COPY src/Ocelot/Ocelot.csproj src/Ocelot/Ocelot.csproj
COPY test/Ocelot.AcceptanceTests/Ocelot.AcceptanceTests.csproj test/Ocelot.AcceptanceTests/Ocelot.AcceptanceTests.csproj
COPY test/Ocelot.ManualTest/Ocelot.ManualTest.csproj test/Ocelot.ManualTest/Ocelot.ManualTest.csproj
COPY test/Ocelot.IntegrationTests/Ocelot.IntegrationTests.csproj test/Ocelot.IntegrationTests/Ocelot.IntegrationTests.csproj
COPY test/Ocelot.UnitTests/Ocelot.UnitTests.csproj test/Ocelot.UnitTests/Ocelot.UnitTests.csproj
COPY test/Ocelot.Benchmarks/Ocelot.Benchmarks.csproj test/Ocelot.Benchmarks/Ocelot.Benchmarks.csproj
RUN dotnet restore

#Now we add the rest of the source and run a complete build
COPY codeanalysis.ruleset codeanalysis.ruleset
COPY src src
COPY test test
ARG build_configuration=Debug
RUN dotnet build -c ${build_configuration} && dotnet publish -c ${build_configuration} && dotnet pack

#Run manual tests!
#docker build --target manual-test -t ocelot-manual-test . && docker run --net host ocelot-manual-test
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS manual-test
WORKDIR /app
ARG build_configuration=Debug
COPY --from=builder  /build/test/Ocelot.ManualTest/bin/${build_configuration}/netcoreapp2.1/publish/ ./
ENTRYPOINT ["dotnet", "Ocelot.ManualTest.dll"]

#Run benchmarks!
#docker build --build-arg build_configuration=Release --target benchmark -t ocelot-benchmark . && docker run ocelot-benchmark {benchmark-name}
#FROM builder AS benchmark
#ENTRYPOINT ["dotnet", "run", "-c", "Release", "--project", "test/Ocelot.Benchmarks/Ocelot.Benchmarks.csproj"]

#Run tests!
#docker build --target unit-tests -t ocelot-tests .
#FROM builder AS unit-tests
#RUN dotnet test --logger:trx;LogFileName=results.trx

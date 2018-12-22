#This is the base image used for any ran images
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

#This image is used to build the source for the runnable app
#It can also be used to run other CLI commands on the project, such as packing/deploying nuget packages. Some examples:
#Run tests: docker build --target builder -t ocelot-build . && docker run ocelot-build test --logger:trx;LogFileName=results.trx
#Run benchmarks: docker build --target builder --build-arg build_configuration=Release -t ocelot-build . && docker run ocelot-build run -c Release --project test/Ocelot.Benchmarks/Ocelot.Benchmarks.csproj
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
#Now we add the rest of the source and run a complete build... --no-restore is used because nuget should be resolved at this point
COPY codeanalysis.ruleset codeanalysis.ruleset
COPY src src
COPY test test
ARG build_configuration=Debug
RUN dotnet build --no-restore -c ${build_configuration}
ENTRYPOINT ["dotnet"]

#This is just for holding the published manual tests...
FROM builder AS manual-test-publish
ARG build_configuration=Debug
RUN dotnet publish --no-build -c ${build_configuration} -o /app test/Ocelot.ManualTest

#Run manual tests! This is the default run option.
#docker build -t ocelot-manual-test . && docker run --net host ocelot-manual-test
FROM base AS manual-test
ENV ASPNETCORE_ENVIRONMENT=Development
COPY --from=manual-test-publish /app .
ENTRYPOINT ["dotnet", "Ocelot.ManualTest.dll"]

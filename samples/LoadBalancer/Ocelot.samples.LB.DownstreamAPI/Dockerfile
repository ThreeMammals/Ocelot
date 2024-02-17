#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["samples/LoadBalancer/Ocelot.samples.LB.DownstreamAPI/Ocelot.samples.LB.DownstreamAPI.csproj", "samples/LoadBalancer/Ocelot.samples.LB.DownstreamAPI/"]
RUN dotnet restore "./samples/LoadBalancer/Ocelot.samples.LB.DownstreamAPI/./Ocelot.samples.LB.DownstreamAPI.csproj"
COPY . .
WORKDIR "/src/samples/LoadBalancer/Ocelot.samples.LB.DownstreamAPI"
RUN dotnet build "./Ocelot.samples.LB.DownstreamAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ocelot.samples.LB.DownstreamAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.samples.LB.DownstreamAPI.dll"]
#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Ocelot.Downstream.Service.Instance1/Ocelot.Samples.OcelotConsulServiceDiscovery.Downstream.csproj", "Ocelot.Downstream.Service.Instance1/"]
COPY ["Ocelot.Sample.LoadBalancer.Common/Ocelot.Samples.OcelotConsulServiceDiscovery.Common.csproj", "Ocelot.Sample.LoadBalancer.Common/"]
RUN dotnet restore "./Ocelot.Downstream.Service.Instance1/./Ocelot.Samples.OcelotConsulServiceDiscovery.Downstream.csproj"
COPY . .
WORKDIR "/src/Ocelot.Downstream.Service.Instance1"
RUN dotnet build "./Ocelot.Samples.OcelotConsulServiceDiscovery.Downstream.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ocelot.Samples.OcelotConsulServiceDiscovery.Downstream.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Samples.OcelotConsulServiceDiscovery.Downstream.dll"]
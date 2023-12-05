#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Ocelot.Sample.ApiGateway/Ocelot.Sample.ApiGateway.csproj", "Ocelot.Sample.ApiGateway/"]
COPY ["src/Ocelot.Provider.Consul/Ocelot.Provider.Consul.csproj", "src/Ocelot.Provider.Consul/"]
COPY ["src/Ocelot/Ocelot.csproj", "src/Ocelot/"]
RUN dotnet restore "./Ocelot.Sample.ApiGateway/./Ocelot.Sample.ApiGateway.csproj"
COPY . .
WORKDIR "/src/Ocelot.Sample.ApiGateway"
RUN dotnet build "./Ocelot.Sample.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ocelot.Sample.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Sample.ApiGateway.dll"]
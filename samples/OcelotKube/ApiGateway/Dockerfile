FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ApiGateway/Ocelot.Samples.OcelotKube.ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/Ocelot.Samples.OcelotKube.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ApiGateway"
RUN dotnet build "Ocelot.Samples.OcelotKube.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ocelot.Samples.OcelotKube.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Samples.OcelotKube.ApiGateway.dll"]

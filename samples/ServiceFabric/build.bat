cd ./src/OcelotApplicationService/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../OcelotApplication/OcelotApplicationServicePkg/Code
cd ../../

cd ./src/OcelotApplicationApiGateway/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../OcelotApplication/OcelotApplicationApiGatewayPkg/Code
cd ../../


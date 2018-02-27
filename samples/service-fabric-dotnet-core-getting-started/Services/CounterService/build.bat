cd ./src/CounterServiceApplication/CounterService/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../../CounterServiceApplication/CounterServicePkg/Code
cd ../../../

cd ./src/CounterServiceApplication/CounterService.WebService/CounterServiceWebService/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../../../CounterServiceApplication/CounterServiceWebServicePkg/Code/
cd ../../../

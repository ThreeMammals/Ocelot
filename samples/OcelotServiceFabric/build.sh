#!/bin/bash
DIR=`dirname $0`
source $DIR/dotnet-include.sh 

cd $DIR/src/OcelotApplicationService/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../OcelotApplication/OcelotApplicationServicePkg/Code
cd -

cd $DIR/src/OcelotApplicationApiGateway/
dotnet restore -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../../OcelotApplication/OcelotApplicationApiGatewayPkg/Code
cd -

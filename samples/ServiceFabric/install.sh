#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

appPkg="$DIR/OcelotServiceApplication"

WebServiceManifestlocation="$appPkg/OcelotApplicationApiGatewayPkg"
WebServiceManifestlocationLinux="$WebServiceManifestlocation/ServiceManifest-Linux.xml"
WebServiceManifestlocationWindows="$WebServiceManifestlocation/ServiceManifest-Windows.xml"
WebServiceManifestlocation="$WebServiceManifestlocation/ServiceManifest.xml"
cp $WebServiceManifestlocationLinux $WebServiceManifestlocation 


StatefulServiceManifestlocation="$appPkg/OcelotApplicationServicePkg"
StatefulServiceManifestlocationLinux="$StatefulServiceManifestlocation/ServiceManifest-Linux.xml"
StatefulServiceManifestlocationWindows="$StatefulServiceManifestlocation/ServiceManifest-Windows.xml"
StatefulServiceManifestlocation="$StatefulServiceManifestlocation/ServiceManifest.xml"
cp $StatefulServiceManifestlocationLinux $StatefulServiceManifestlocation
cp dotnet-include.sh ./OcelotServiceApplication/OcelotApplicationServicePkg/Code
cp dotnet-include.sh ./OcelotServiceApplication/OcelotApplicationApiGatewayPkg/Code
sfctl application upload --path OcelotServiceApplication --show-progress
sfctl application provision --application-type-build-path OcelotServiceApplication
sfctl application create --app-name fabric:/OcelotServiceApplication --app-type OcelotServiceApplicationType --app-version 1.0.0

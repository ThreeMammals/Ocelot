---
services: service-fabric
platforms: dotnet
author: raunakpandya edited by Tom Pallister for Ocelot
---

# Ocelot Service Fabric example

This shows a service fabric cluster with Ocelot exposed over HTTP accessing services in the cluster via the naming service. If you want to try and use Ocelot with
Service Fabric I reccomend using this as a starting point.

If you want to use statefull / actors you must send the PartitionKind and PartitionKey to Ocelot as query string parameters.

I have not tested this sample on Service Fabric hosted on Linux just a Windows dev cluster. This sample assumes a good understanding of Service Fabric.

The rest of this document is from the Microsoft asp.net core service fabric getting started guide.

# Getting started with Service Fabric with .NET Core

This repository contains a set of simple sample projects to help you getting started with Service Fabric on Linux with .NET Core as the framework. As a pre requisite ensure you have the Service Fabric C# SDK installed on ubuntu box. Follow these instruction to [prepare your development environment on Linux][service-fabric-Linux-getting-started]

### Folder Hierarchy
* src/ - Source of the application divided by different modules by sub-folders.  
* &lt;application package folder&gt;/ - Service Fabric Application folder heirarchy. After compilation the executables are placed in code subfolders.  
* build.sh - Script to build source on Linux shell.  
* build.ps1 - PowerShell script to build source on Windows.  
* install.sh - Script to install Application from Linux shell.  
* install.ps1 - PowerShell script to install application from Windows.  Before calling this script run Connect-ServiceFabricCluster localhost:19000 or however you prefer to connect.
* uninstall.sh - Script to uninstall application from Linux shell.  
* uninstall.ps1 - PowerShell script to unintall application from Windows.
* dotnet-include.sh - Script to conditionally handle RHEL dotnet cli through scl(software collections)

# Testing

Once everything is up and running on your dev cluster visit http://localhost:31002/EquipmentInterfaces and you should see the following returned.

```json
["value1","value2"]
```

If you get any errors please check the service fabric logs and let me know if you need help.

## More information

The [Service Fabric documentation][service-fabric-docs] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

<!-- Links -->

[service-fabric-programming-models]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-choose-framework/
[service-fabric-docs]: http://aka.ms/servicefabricdocs
[service-fabric-Linux-getting-started]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started-linux/

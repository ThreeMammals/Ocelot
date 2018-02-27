---
services: service-fabric
platforms: dotnet
author: raunakpandya
---

# Getting started with Service Fabric with .NET Core

This repository contains a set of simple sample projects to help you getting started with Service Fabric on Linux with .NET Core as the framework. As a pre requisite ensure you have the Service Fabric C# SDK installed on ubuntu box. Follow these instruction to [prepare your development environment on Linux][service-fabric-Linux-getting-started]

## How the samples are organized

The samples are divided by the category and [Service Fabric programming model][service-fabric-programming-models] that they focus on: Reliable Actors, Reliable Services. Most real applications will include a mixture of the concepts and programming models.

### Folder Hierarchy
* src/ - Source of the application divided by different modules by sub-folders.  
* &lt;application package folder&gt;/ - Service Fabric Application folder heirarchy. After compilation the executables are placed in code subfolders.  
* build.sh - Script to build source on Linux shell.  
* build.ps1 - PowerShell script to build source on Windows.  
* install.sh - Script to install Application from Linux shell.  
* install.ps1 - PowerShell script to install application from Windows.  
* uninstall.sh - Script to uninstall application from Linux shell.  
* uninstall.ps1 - PowerShell script to unintall application from Windows.
* dotnet-include.sh - Script to conditionally handle RHEL dotnet cli through scl(software collections)

## Reliable Actor samples
### CounterActor

Counter Actor provides an example of a very simple actor which implements a counter. Once the service is deployed you can run the testclient to see the output of the counter on console. 
The application includes a OWIN self hosting based web service, modeled as Service Fabric reliable stateless service, accessible at http://&lt;clusteraddress&gt;:31001. where you can see the effect of counter incrementing on the web UI.

### CalculatorActor

Calculator Actor sample uses the actor programming model to implement two basic calculator operations, add and subtract. Once the service is deployed you can run the testclient to see the output of the calculator on console.

## Reliable Service samples
### CounterService

Counter Service provides an example of a stateful service which implements a counter.
The application includes a OWIN self hosting based web service, modeled as Service Fabric reliable stateless service, accessible at http://&lt;clusteraddress&gt;:31002. where you can see the effect of counter incrementing on the web UI.

## Compiling the samples
For compiling the samples use the build.sh script provided along with the sample which will use .NET core framework configured as part of the Service Fabric installation to compile the sample.

## Deploying the samples
All the samples once compiled can be deployed immediately using the install.sh script provided along with the sample. These scripts underneath uses azurecli. Before running the scripts you need to first connect to the cluster using azurecli.

## More information

The [Service Fabric documentation][service-fabric-docs] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

<!-- Links -->

[service-fabric-programming-models]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-choose-framework/
[service-fabric-docs]: http://aka.ms/servicefabricdocs
[service-fabric-Linux-getting-started]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started-linux/

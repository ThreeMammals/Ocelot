## Upgrade to .NET 8 (version {0}) aka [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) release
> Read article: [Announcing .NET 8](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8/) by Gaurav Seth, on November 14th, 2023

### About
We are pleased to announce to you that we can now offer the support of [.NET 8](https://dotnet.microsoft.com/en-us/download).
But that is not all, in this release, we are adopting support of several versions of the .NET framework through [multitargeting](https://learn.microsoft.com/en-us/dotnet/standard/frameworks).
The Ocelot distribution is now compatible with .NET **6**, **7** and **8**. :tada:

In the future, we will try to ensure the support of the [.NET SDKs](https://dotnet.microsoft.com/en-us/download/dotnet) that are still actively maintained by the .NET team and community.
Current .NET versions in support are the following: [6, 7, 8](https://dotnet.microsoft.com/en-us/download/dotnet).

### Technical info
As an ASP.NET Core app, now Ocelot targets `net6.0`, `net7.0` and `net8.0` frameworks.

Starting with **v{0}**, the solution's code base supports [Multitargeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-multitargeting-overview) as SDK-style projects.
It should be easier for teams to move between (migrate to) .NET 6, 7 and 8 frameworks. Also, new features will be available for all .NET SDKs which we support via multitargeting.
Find out more here: [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)

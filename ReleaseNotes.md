## :package: End of .NET 6, 7 Support (version {0})
> Read the Docs: [Ocelot 23.4](https://ocelot.readthedocs.io/en/{0}/) with [PDF](https://ocelot.readthedocs.io/_/downloads/en/{0}/pdf/)
> Hot fixed version: [{1}](https://github.com/ThreeMammals/Ocelot/releases/tag/{1})
> Milestone: [November'24](https://github.com/ThreeMammals/Ocelot/milestone/10)

This is the last patched version for .NET 6 and 7 frameworks. The upcoming major release, version [24.0](https://github.com/ThreeMammals/Ocelot/milestone/11), will target .NET 9 alongside the LTS .NET 8. Projects targeting .NET 6 or 7 should update to this version while considering an upgrade to .NET 8 or 9 in the future.

### :information_source: About 
- All package versions have been updated to latest versions targeting the `net6.0` and `net7.0` frameworks, along with the LTS `net8.0`.
- Dependabot alerts concerning [reported vulnerabilities](https://github.com/ThreeMammals/Ocelot/security/dependabot) related to `IdentityServer4` have not yet been addressed; these will be resolved in the next major release (refer to **Warnings** further information).

### :exclamation: Warning  
1. Releasing a patched `23.4.*` is possible.  
2. In the next major version, the Ocelot team will remove references to the [IdentityServer4](https://github.comIdentityServer/IdentityServer4) package from testing projects due to its "Public Archive" status; the version, [4.1.2](https://github.comIdentityServer/IdentityServer4/releases/tag/4.1.2), was [released](https://github.com/IdentityServer/IdentityServer4/releases) on July 7, 2021.  
3. The main Ocelot package is not integrated with `IdentityServer4`, allowing Ocelot users to utilize [any authentication provider](https://github.com/ThreeMammals/Ocelot/discussions/2194), as Ocelot's [Authentication](https://github.com/ThreeMammals/Ocelot/blob/main/docs/authentication.rst) feature is provider-agnostic.  
4. Our plans to utilize the [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) framework in testing projects due to industry standards, instead the `IdentityServer4` library.  
5. Following the release of .NET 9, the team will begin the deprecation of the Ocelot extension-packages: `Ocelot.Cache.CacheManager`, `Ocelot.Tracing.Butterfly`, and `Ocelot.Tracing.OpenTracing`.

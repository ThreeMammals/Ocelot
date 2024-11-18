## :twisted_rightwards_arrows: Routing Update (version {0}) aka [MGGA](https://github.com/ThreeMammals/Ocelot/commits?author=ggnaegi) release
> Codenamed: **[Make Guillaume Great Again!](https://github.com/ThreeMammals/Ocelot/commits?author=ggnaegi)**
> Read the Docs: [Ocelot {0}](https://ocelot.readthedocs.io/en/{0}/)

### :information_source: About
This minor release significantly upgrades the [Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst) feature by supporting [embedded placeholders](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#embedded-placeholders-1) within path segments (between slashes). Additionally, the team has focused on enhancing the performance of `Regex` objects.

### :new: What's new?
- **[Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst)**: Introducing the new "[Embedded Placeholders](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#embedded-placeholders-1)" feature by @ggnaegi.
  As of November 2024, Ocelot was unable to process multiple [placeholders](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#placeholders) embedded between two forward slashes. It was also challenging to differentiate the placeholder from other elements within the slashes. For example, `/{{url}}-2/` for `/y-2/` would yield `{{url}} = y-2`. We are excited to introduce an enhanced method for evaluating placeholders that allows for the resolution of [placeholders](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#placeholders) within complex URLs.
  For additional information, refer to PR #2200.

### :up: Focus On
<details>
  <summary><b>Features</b>: Routing, Core, Rate Limiting, Middleware Injection</summary>

  - [Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst): The new feature is "[Embedded Placeholders](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#embedded-placeholders-1)" by @ggnaegi.
  - [Core](https://github.com/ThreeMammals/Ocelot/labels/Core): All `Regex` logic has been refactored by @EngRajabi.
    The Ocelot Core now boasts improved performance of `Regex` objects, striving to adhere to the [Best Practices for Regular Expressions in .NET](https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices). It is estimated that each request could save from 1 to over 10 microseconds in processing time (though no benchmarks have been developed to measure this).
  - [Rate Limiting](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/ratelimiting.rst): The persistent issue with [Rate Limiting headers](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/ratelimiting.rst#global-configuration:~:text=headers) has been resolved by @jlukawska.
    The problem was the absence of unofficial `X-Rate-Limit-*` headers (found in the [RateLimitingHeaders](https://github.com/ThreeMammals/Ocelot/blob/{0}/src/Ocelot/RateLimiting/RateLimitingHeaders.cs) class) in the `RateLimitingMiddleware`'s response. For more details, see PR #1307.
    Note that these unofficial headers have not yet been documented, so they may be subject to change since [Ocelot's RateLimiting headers do not align with industry standards, see links](https://github.com/ThreeMammals/Ocelot/blob/27d3df2d0fdfbf5acde12d9442dfc08836e8b982/src/Ocelot/RateLimiting/RateLimitingHeaders.cs#L6).
  - [Middleware Injection](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/middlewareinjection.rst): The `OcelotPipelineConfiguration.ClaimsToHeadersMiddleware` property has been introduced by @kesskalli.
    This new property enables the overriding of the [ClaimsToHeadersMiddleware](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/middlewareinjection.rst#middleware-injection:~:text=ClaimsToHeadersMiddleware). For additional information, refer to PR #1403.
</details>

<details>
  <summary><b>Documentation</b> for <a href="https://ocelot.readthedocs.io/en/{0}/">v{0}</a></summary>

  - [Routing](https://ocelot.readthedocs.io/en/{0}/features/routing.html): Introducing a new section on [Embedded Placeholders](https://ocelot.readthedocs.io/en/{0}/features/routing.html#embedded-placeholders)
  - [Middleware Injection](https://ocelot.readthedocs.io/en/{0}/features/middlewareinjection.html): Documentation now includes the [ClaimsToHeadersMiddleware](https://ocelot.readthedocs.io/en/{0}/features/middlewareinjection.html#middleware-injection:~:text=ClaimsToHeadersMiddleware) feature
</details>

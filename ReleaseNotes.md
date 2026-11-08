<!--
{{0}} -> {0}
{{1}} -> {1}
Tag to substitute: {0}
https://www.nuget.org/packages/Ocelot/{0}
-->

## Autumn 2026 (version 25.1) a.k.a. "Rise of Ocelot" release
> **Milestone: [Autumn'26](https://github.com/ThreeMammals/Ocelot/milestone/12)**
> Codenamed: **[Rise of Ocelot](https://github.com/ThreeMammals/Ocelot/milestone/12)**
> Read the Docs: [Ocelot 25.1](https://ocelot.readthedocs.io/en/25.1/) with [PDF](https://ocelot.readthedocs.io/_/downloads/en/25.1/pdf/)

### :information_source: About

> [!WARNING]
> Version [25.1](https://github.com/ThreeMammals/Ocelot/milestone/12) is in development!
> Full changelog: [release/25.0...develop](https://github.com/ThreeMammals/Ocelot/compare/release/25.0...develop)
> Read the latest **25.0** release notes here: [v25.0](https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.2)

### :new: What's New?

* [HTTP Handler](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst#httphandleroptions): The *"Pass-through Credentials"* feature was added by @Sacrilege in pull request #1521.

  This feature is the result of updates to three Ocelot features: [Configuration](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst), [Authentication](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authentication.rst), and [Authorization](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst). Note that the *HTTP Handler*, a `SocketsHttpHandler` object, is created by `MessageInvokerPool`.

  * [Configuration](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst): The [HTTP Handler options](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst#httphandleroptions) schema was extended for downstream credential handling.

    The configuration model now exposes `UseCredentials` for route-level *HTTP handler* setup, allowing Ocelot to initialize `SocketsHttpHandler.Credentials` with the default network credentials used by the current execution context.
    This complements the [Authentication](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authentication.rst) and [Authorization](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst) features by enabling Windows-authenticated downstream calls to be configured per route without changing the broader gateway behavior.

  * [Authentication](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authentication.rst): The "[Pass Through Authentication](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authentication.rst#pass-through-authentication-6)" feature.

    When [HTTP Handler options](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst#httphandleroptions) `UseCredentials` is enabled for a route, Ocelot initializes `SocketsHttpHandler.Credentials` to use default credentials for the outbound HTTP request.
    This is especially useful for Windows Integrated Authentication scenarios, including NTLM and Kerberos, where the downstream service or proxy expects the caller's identity to be propagated without explicitly setting credentials in code.
    The setting is scoped to the route, which keeps authentication behavior predictable and avoids forcing a global credential policy across every downstream service.
    For .NET developers, this is the same general pattern described by [WebClient.UseDefaultCredentials](https://learn.microsoft.com/en-us/dotnet/api/system.net.webclient.usedefaultcredentials), but applied at the Ocelot [HTTP Handler](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst#httphandleroptions) level for downstream requests.

  * [Authorization](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst): Route-level credential configuration was aligned with authorization scenarios through the "[Pass Through Authorization](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst#pass-through-authorization-1)" feature.

    Authorization rules remain claim-based and route-scoped, while `UseCredentials` controls how the downstream HTTP client authenticates to the target service. This allows Windows-authenticated routes to coexist with existing route policy checks and claim requirements without altering the authorization model itself.

* [Aggregation](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/aggregation.rst): The "[Upstream HTTP Methods](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/aggregation.rst#upstream-http-methods-4)" feature was added by @crb02005 in pull request #1389.

  Aggregate routes can now accept HTTP methods other than `GET` and proxy/forward request bodies to downstream services.
  The `UpstreamHttpMethod` option is available per aggregate route and globally in `GlobalConfiguration`, and the two lists are merged.
  The default method, used as a fallback when JSON configuration is absent, can also be customized in C# by overriding the `DefaultHttpMethod` property of `AggregatesCreator`.

[RFC 7578]: https://www.rfc-editor.org/info/rfc7578/
[Content-Type]: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Type

* [Request Proxying](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/request-proxying.rst): The `multipart/form-data` [Content-Type] header handling was improved by @rodri-oliveira-dev in pull request #2421.

  Ocelot now preserves multipart boundaries and file payloads when routing and multiplexing (aggregation) upload requests across downstream services, and the new documentation explains the supported scenarios and validation coverage for file uploads.
  This feature is especially useful for BFF scenarios where a single upstream request may need to be routed to multiple downstream services, each of which may require access to the same uploaded files or form data.
  Thorough testing was performed to ensure that Ocelot correctly handles multipart requests following [RFC 7578] specification.

### :up: What's Updated?

*To be added...*

### :package: Patches

[RFC 9110]: https://www.rfc-editor.org/rfc/rfc9110#section-15.6.5

* [Error Codes](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/errorcodes.rst): Issue #1687 was patched by @rodri-oliveira-dev in pull request #2420.

  Ocelot now returns `504 Gateway Timeout` when a downstream request times out, in accordance with [RFC 9110]. Client-request cancellation behavior and existing QoS/circuit-breaker behavior remain unchanged. Related timeout tests and documentation were updated accordingly.

* [Authorization](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst): Issue #679 was patched by @Ommmarr111 in pull request #2414.

  `RouteClaimsRequirement` authorization options now preserve URL-shaped claim type keys such as `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` when loaded from configuration. ASP.NET Core's binder previously split keys on `:` and silently discarded the requirement, causing standard .NET claim types to stop matching routes. The fix reconstructs the original keys after binding so authorization policies continue to work correctly.

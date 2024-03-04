## January 2024 (version {0}) aka [Hornussen](https://www.myswitzerland.com/en-ch/planning/about-switzerland/custom-and-tradition/hornussen-where-the-nouss-flies-from-the-ramp-and-into-the-playing-field/) release
> Codenamed as **[Hornussen Sport](https://www.youtube.com/results?search_query=Hornussen)**
> Read the Docs: [Ocelot 23.1](https://ocelot.readthedocs.io/en/23.1.0/)

### Focus On

<details>
  <summary><b>Multiplexing middleware</b> aka <a href="https://ocelot.readthedocs.io/en/latest/features/requestaggregation.html">Request Aggregation</a> feature</summary>
 
- Significant refactoring and design review of the [Multiplexer](https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot/Multiplexer)
- Optimizing multiplexer performance: `HttpContext` is not copied when there is only one downstream route, and etc.
- Fixed [the bug](https://github.com/ThreeMammals/Ocelot/pull/1462) in the multiplexer: `HttpContext.User` information was not copied if there was more than one downstream request.
</details>

<details>
  <summary><b>System routing</b>. Content streaming when <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Transfer-Encoding">Transfer-Encoding</a>: 'chunked'</summary>

  - Correction of [the bug](https://github.com/ThreeMammals/Ocelot/pull/1972) when creating requests: The header [Transfer-Encoding](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Transfer-Encoding): `chunked` was present even when there was no content or the request body size was 0. These cases are now addressed.
</details>

<details>
  <summary><b>Updates of the features</b>: QoS, Load Balancer and Error Status Codes</summary>
 
- [Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html): Possibility of implementation of custom Polly v8.2 providers. New `AddPolly` extension methods.
- [Load Balancer](https://ocelot.readthedocs.io/en/latest/features/loadbalancer.html): Extension of the route key format, ensuring that the key remains unique for cases of **UpstreamHost** route property and **ServiceName** vs **ServiceNamespace** properties in Consul setup.
- [Error Status Codes](https://ocelot.readthedocs.io/en/latest/features/errorcodes.html): When [413 Content Too Large](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/413), Ocelot now returns a 413 `PayloadTooLargeError` (Ocelot error code `41`).
</details>

<details>
  <summary>Documentation for <b>Request Aggregation</b></summary>
 
  - [Request Aggregation](https://ocelot.readthedocs.io/en/latest/features/requestaggregation.html)
</details>

<details>
  <summary><b>Stabilization</b> aka bug fixing</summary>

  - See [all bugs](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+is%3Aclosed+label%3Abug+milestone%3AJanuary%2724) of the [January'24](https://github.com/ThreeMammals/Ocelot/milestone/4) milestone
</details>

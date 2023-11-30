## October 2023 (version {0}) aka [Swiss Locomotive](https://en.wikipedia.org/wiki/SBB-CFF-FFS_Ae_6/6) release
> Codenamed as **[Swiss Locomotive](https://www.google.com/search?q=swiss+locomotive)**

### Focused On
<details>
  <summary><b>Logging feature</b>. Performance review, redesign and improvements with new best practices to log</summary>

  - Proposing a centralized `WriteLog` method for the `OcelotLogger`
  - Factory methods for computed strings such as `string.Format` or interpolated strings
  - Using `ILogger.IsEnabled` before calling the native `WriteLog` implementation and invoking string factory method
</details>
<details>
  <summary><b>Quality of Service feature</b>. Redesign and stabilization, and it produces less log records now.</summary>
 
  - Fixing issue with [Polly](https://www.thepollyproject.org/) Circuit Breaker not opening after max number of retries reached
  - Removing useless log calls that could have an impact on performance
  - Polly [lib](https://www.nuget.org/packages/Polly#versions-body-tab) reference updating to latest `8.2.0` with some code improvements
</details>
<details>
  <summary>Documentation for <b>Logging</b>, <b>Request ID</b>, <b>Routing</b> and <b>Websockets</b></summary>
 
  - [Logging](https://ocelot.readthedocs.io/en/latest/features/logging.html)
  - [Request ID](https://ocelot.readthedocs.io/en/latest/features/requestid.html)
  - [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html)
  - [Websockets](https://ocelot.readthedocs.io/en/latest/features/websockets.html)
</details>
<details>
  <summary>Testing improvements and stabilization aka <b>bug fixing</b></summary>

  - [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html) bug fixing: query string placeholders including **CatchAll** one aka `{{everything}}` and query string duplicates removal
  - [QoS](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html) bug fixing: Polly circuit breaker exceptions
  - Testing bug fixing: rare failed builds because of unstable Polly tests. Acceptance common logic for ports
</details>

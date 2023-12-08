## Hotfix release (version {0})
> Default timeout vs the [Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html) feature

Special thanks to **Alvin Huang**, @huanguolin!

### About
The bug is related to the **Quality of Service** feature (aka **QoS**) and the [HttpClient.Timeout](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout) property.
- If JSON `QoSOptions` section is defined in the route config, then the bug is masked rather than active, and the timeout value is assigned from the [QoS TimeoutValue](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html#quality-of-service:~:text=%22TimeoutValue%22%3A%205000) property.
- If the `QoSOptions` section **is not** defined in the route config or the [TimeoutValue](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html#quality-of-service:~:text=%22TimeoutValue%22%3A%205000) property is missing, then the bug is **active** and affects downstream requests that **never time out**.

### Technical info
In version [22.0](https://github.com/ThreeMammals/Ocelot/releases/tag/22.0.0), the bug was found in the explicit default constructor of the [FileQoSOptions](https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileQoSOptions.cs) class with a maximum [TimeoutValue](https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileQoSOptions.cs#L9). Previously, the default constructor was implicit with the default assignment of zero `0` to all `int` properties.

The new explicit default constructor breaks the old implementation of [QoS TimeoutValue](https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Requester/HttpClientBuilder.cs#L53-L55) logic, as our [QoS documentation](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html#quality-of-service:~:text=If%20you%20do%20not%20add%20a%20QoS%20section%2C%20QoS%20will%20not%20be%20used%2C%20however%20Ocelot%20will%20default%20to%20a%2090%20seconds%20timeout%20on%20all%20downstream%20requests.) states:
![image](https://github.com/ThreeMammals/Ocelot/assets/12430413/2c6b2cd3-e1c6-4510-9e46-883468c140ec) <br/>
**Finally**, the "default 90 second" logic for `HttpClient` breaks down when there are no **QoS** options and all requests on those routes are infinite, if, for example, downstream services are down or stuck.

#### The Bug Artifacts
- Reported bug issue: [1833](https://github.com/ThreeMammals/Ocelot/issues/1833) by @huanguolin
- Hotfix PR: [1834](https://github.com/ThreeMammals/Ocelot/pull/1834) by @huanguolin

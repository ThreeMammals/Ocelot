## 🔥 Hot fixing v[23.3](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0) (version {0}) aka [Blue Olympic Balumbes](https://www.youtube.com/live/j-Ou-ggS718?si=fPPwmOwjYEZq70H9&t=9518) release
> Codenamed: **[Blue Olympic Fiend](https://www.youtube.com/live/j-Ou-ggS718?si=fPPwmOwjYEZq70H9&t=9518)**
> Read the Docs: [Ocelot 23.3](https://ocelot.readthedocs.io/en/{0}/)
> Hot fixed versions: [23.3.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0), [23.3.3](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.3)
> Milestone: [v23.3 Hotfixes](https://github.com/ThreeMammals/Ocelot/milestone/8)

❤️ A heartfelt "Thank You" to [Roman Shevchik](https://github.com/antikorol) and [Massimiliano Innocenti](https://github.com/minnocenti901) for their contributions in testing and reporting the [Service Discovery](https://github.com/ThreeMammals/Ocelot/labels/Service%20Discovery) issues, #2110 and #2119, respectively!

### ℹ️ About
This release delivers a number of bug fixes for the predecessor's [23.3.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0) release, which is full of new features but was not tested well. All bugs were combined into the [v23.3 Hotfixes](https://github.com/ThreeMammals/Ocelot/milestone/8) milestone.

Following the substantial refactoring of [Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst) providers in the [23.3.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0) release, the community identified and we have acknowledged several [critical service discovery defects](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+milestone%3A%22v23.3+Hotfixes%22+label%3A%22Service+Discovery%22) with providers such as [Kube](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/kubernetes.rst) and [Consul](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#consul). The `Kube` provider, while somewhat unstable, remained operational; however, the `Consul` provider was entirely non-functional.

📓 If your projects rely on the [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html) feature and cannot function without it, please upgrade to this version to utilize the full list of features of version [23.3.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0).

### 🧑‍💻 Technical Information
A comprehensive explanation of the technical details would span several pages; therefore, it is advisable for fans of Ocelot to review all pertinent technical information within the issue descriptions associated with [the milestone](https://github.com/ThreeMammals/Ocelot/milestone/8).
Our team has implemented some **Breaking Changes** which we urge you to review carefully (details follow).

### ⚠️ Breaking Changes
Listed by priority:
- `ILoadBalancer` interface alteration: Method `Lease` is now `LeaseAsync`.
  Interface FQN: `Ocelot.LoadBalancer.LoadBalancers.ILoadBalancer`
  Method FQN: `Ocelot.LoadBalancer.LoadBalancers.ILoadBalancer.LeaseAsync`
- `DefaultConsulServiceBuilder` constructor modification: The first parameter's type has been changed from `Func<ConsulRegistryConfiguration>` to `IHttpContextAccessor`.
  Class FQN: `Ocelot.Provider.Consul.DefaultConsulServiceBuilder`
  Constructor signature: `public DefaultConsulServiceBuilder(IHttpContextAccessor contextAccessor, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)`
- Adjustments to `Lease` type: The `Lease` has been restructured from a class to a structure and elevated in the namespace hierarchy.
  Struct FQN: `Ocelot.LoadBalancer.Lease`

📓 Should your [custom solutions](https://ocelot.readthedocs.io/en/latest/search.html?q=custom) involve overriding default Ocelot classes and their behavior, redevelopment or at least recompilation of the solution, followed by deployment, will be necessary.

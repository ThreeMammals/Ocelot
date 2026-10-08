.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
.. _25.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
.. _25.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.1
.. _25.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
.. _.NET 8: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
.. _.NET 9: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
.. _.NET 10: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
.. _Ocelot: https://www.nuget.org/packages/Ocelot
.. _Ocelot.Testing: https://github.com/ThreeMammals/Ocelot.Testing
.. _DevOps: https://github.com/ThreeMammals/Ocelot/labels/DevOps
.. role::  htm(raw)
    :format: html

.. _welcome:

#######
Welcome
#######

.. Welcome to the Ocelot `25.0`_ documentation!

.. warning::
  | Version `25.1 <https://github.com/ThreeMammals/Ocelot/milestone/12>`_ is in development! Full changelog: `release/25.0...develop <https://github.com/ThreeMammals/Ocelot/compare/release/25.0...develop>`_
  | Read the latest **25.0** documentation here: `v25.0 <https://ocelot.readthedocs.io/en/25.0/>`_

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major, minor or patched versions.

.. The major version `25.0.0`_ includes several patches, the history of which is outlined below.

.. .. admonition:: Patches

..   - `25.0.1`_, on September 14, 2026: Reference SDK `10.0.401 <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>`_ (Runtime `10.0.12 <https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md>`_)

.. _release-notes:

📢 Release Notes
-----------------

  | Release Tag: `25.1.0`_ (to be created)
  | Release Codename: `Rise of Ocelot <https://github.com/ThreeMammals/Ocelot/milestone/12>`_
  | Release Date: ... 2026

To be added...

🆕 What's New?
----------------
.. _@raman-m: https://github.com/raman-m
.. _@ocelot-ot: https://github.com/ocelot-ot
.. _@erannevo: https://github.com/erannevo
.. _@CurtisRobertOliver: https://github.com/CurtisRobertOliver
.. _@jlukawska: https://github.com/jlukawska
.. _2384: https://github.com/ThreeMammals/Ocelot/issues/2384
.. _2385: https://github.com/ThreeMammals/Ocelot/pull/2385
.. _2386: https://github.com/ThreeMammals/Ocelot/issues/2386
.. _2387: https://github.com/ThreeMammals/Ocelot/pull/2387
.. _2403: https://github.com/ThreeMammals/Ocelot/issues/2403
.. _2406: https://github.com/ThreeMammals/Ocelot/pull/2406
.. _651: https://github.com/ThreeMammals/Ocelot/issues/651
.. _1183: https://github.com/ThreeMammals/Ocelot/pull/1183
.. _2248: https://github.com/ThreeMammals/Ocelot/issues/2248
.. _2328: https://github.com/ThreeMammals/Ocelot/pull/2328
.. _Ocelot.QualityOfService.Polly: https://www.nuget.org/packages/Ocelot.QualityOfService.Polly/
.. _Ocelot.Discovery.KubeClient: https://www.nuget.org/packages/Ocelot.Discovery.KubeClient/
.. _248: https://github.com/ThreeMammals/Ocelot/pull/248
.. _310: https://github.com/ThreeMammals/Ocelot/pull/310
.. _@TomPallister: https://github.com/TomPallister

{ features in development } 

.. * :doc:`../features/qualityofservice`: A ":ref:`Built-in Circuit Breaker <qos-builtin>`" feature implementation was added by `@ocelot-ot`_ in pull request `2385`_ — no `Polly`_ required.

..   Ocelot's :ref:`qos-schema` no longer strictly depends on the external `Ocelot.QualityOfService.Polly`_ package to provide circuit breaking and timeout enforcement.
..   A lightweight, thread-safe circuit breaker (``Closed`` → ``Open`` → ``HalfOpen`` → ``Closed``) ships in the Ocelot core package, supporting both :ref:`count-based <qos-builtin-cb-count-mode>` and :ref:`FailureRatio-based <qos-builtin-cb-ratio-mode>` modes.
..   The :ref:`two implementations <qos-implementations-overview>` are mutually exclusive: the last of ``AddQualityOfService()`` or ``AddPolly()`` registered on the ``OcelotBuilder`` wins.
..   See the :doc:`../features/qualityofservice` documentation for full details, including the ``AddQualityOfService<THandler>()`` extensibility point for :ref:`overriding server error codes <qos-builtin-server-errors-override>`.

.. * :doc:`../features/websockets`: The ":ref:`Overridable WebSocket buffer size <ws-sample>`" feature and custom middleware injection were added by `@erannevo`_ in pull request `2387`_.

..   The previously hard-coded ``DefaultWebSocketBufferSize`` is now a ``protected virtual`` property that can be overridden by subclassing.
..   The :doc:`../features/middlewareinjection` feature was extended with a ``WebSocketsProxyMiddleware`` override on the ``OcelotPipelineConfiguration`` class, allowing a fully custom WebSocket middleware to be injected into the pipeline.
..   Refer to the :doc:`../features/websockets` documentation to understand how to utilize the :ref:`new feature <ws-sample>`.

.. * :doc:`../features/websockets`: The ":ref:`SecurityOptions support <ws-supported>` for the WebSocket pipeline" feature was added by `@CurtisRobertOliver`_ in pull request `2406`_.

..   Previously, IP allow/block-list :doc:`../features/routing` :ref:`security options <routing-security-options>` were not enforced for WebSocket upgrade requests (a.k.a. the ``CONNECT`` HTTP method), allowing them to bypass IP security.
..   WebSocket upgrade requests are now intercepted by the same ``IPSecurityPolicy`` used for regular HTTP requests, and denied upgrades now correctly return ``403 Forbidden``.

.. * :doc:`../features/configuration`: The ":ref:`Extend with Custom Properties <config-custom-properties>`" feature was implemented by `@jlukawska`_ in pull request `1183`_.

..   Ocelot's configuration builder now :ref:`merges custom (non-schema) JSON properties <config-merging-recipes>` defined across multiple ``ocelot.*.json`` files using Newtonsoft's ``JToken`` merge functionality, instead of the last-loaded file silently overwriting properties from previous files in the ``Routes`` collection.
..   This also applies to the ``DynamicRoutes`` collection and the ``GlobalConfiguration`` section.
..   See the updated :doc:`../features/configuration` chapter, especially the ":ref:`Extend with Custom Properties <config-custom-properties>`" section, and the Configuration :ref:`config-sample` app for details.

.. * :doc:`../features/aggregation`: A new ":ref:`Manual/custom aggregators <agg-complex-aggregator>` for :ref:`Complex Aggregation <agg-complex>`" feature has been published (originally developed by `@TomPallister`_ in pull requests `248`_, `310`_).

..   While working on bug `2248`_ in pull request `2328`_, the development team uncovered an undocumented feature in the ``Ocelot.Multiplexer`` namespace and decided to publish this pilot feature in the ":ref:`agg-complex-aggregator`" documentation.
..   The feature is based on the ``IResponseAggregator`` interface.
..   The Ocelot team will test it further in upcoming releases, develop best practices, create a sample app, and inform the community.
..   For this reason the feature is still in pilot status.

🆙 What's Updated?
--------------------
.. _Ocelot.Provider.Kubernetes: https://www.nuget.org/packages/Ocelot.Provider.Kubernetes/
.. _Ocelot.Cache.CacheManager: https://www.nuget.org/packages/Ocelot.Cache.CacheManager/
.. _Ocelot.Tracing.Butterfly: https://www.nuget.org/packages/Ocelot.Tracing.Butterfly/
.. _Ocelot.Tracing.OpenTracing: https://www.nuget.org/packages/Ocelot.Tracing.OpenTracing/
.. _Ocelot.Discovery.Eureka: https://www.nuget.org/packages/Ocelot.Discovery.Eureka/
.. _Ocelot.Discovery.Consul: https://www.nuget.org/packages/Ocelot.Discovery.Consul/
.. _Obsolete attributes: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%5BObsolete&type=code
.. _2334: https://github.com/ThreeMammals/Ocelot/issues/2334
.. _2360: https://github.com/ThreeMammals/Ocelot/pull/2360
.. _2361: https://github.com/ThreeMammals/Ocelot/pull/2361
.. _2362: https://github.com/ThreeMammals/Ocelot/pull/2362
.. _2371: https://github.com/ThreeMammals/Ocelot/issues/2371
.. _2372: https://github.com/ThreeMammals/Ocelot/pull/2372
.. _2378: https://github.com/ThreeMammals/Ocelot/issues/2378
.. _2380: https://github.com/ThreeMammals/Ocelot/pull/2380
.. _2388: https://github.com/ThreeMammals/Ocelot/pull/2388
.. _2404: https://github.com/ThreeMammals/Ocelot/pull/2404
.. _2358: https://github.com/ThreeMammals/Ocelot/pull/2358
.. _2354: https://github.com/ThreeMammals/Ocelot/pull/2354
.. _2356: https://github.com/ThreeMammals/Ocelot/pull/2356
.. _2364: https://github.com/ThreeMammals/Ocelot/pull/2364
.. _2365: https://github.com/ThreeMammals/Ocelot/pull/2365
.. _2359: https://github.com/ThreeMammals/Ocelot/pull/2359
.. _2367: https://github.com/ThreeMammals/Ocelot/pull/2367
.. _2389: https://github.com/ThreeMammals/Ocelot/pull/2389
.. _2392: https://github.com/ThreeMammals/Ocelot/pull/2392
.. _2402: https://github.com/ThreeMammals/Ocelot/pull/2402
.. _2409: https://github.com/ThreeMammals/Ocelot/pull/2409
.. _Visual Studio 2026 Solution File Format: https://devblogs.microsoft.com/visualstudio/new-simpler-solution-file-format/
.. _.NET 10 CLI solution file format: https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/
.. _@methran1304: https://github.com/methran1304

{ updates in development } 

.. * `Extension packages <https://www.nuget.org/profiles/ThreeMammals>`_ extraction `2334`_: The Ocelot team continued extracting `integrated extension packages <https://www.nuget.org/profiles/ThreeMammals>`_ out of `the monorepo <https://github.com/ThreeMammals/Ocelot/tree/24.1.0/src>`_ into their own `dedicated repositories <https://github.com/orgs/ThreeMammals/repositories?q=Ocelot.>`_, each with an independent release cycle:

..   * `Ocelot.Cache.CacheManager`_ in pull request `2360`_
..   * `Ocelot.Tracing.Butterfly`_ in pull request `2361`_
..   * `Ocelot.Tracing.OpenTracing`_ in pull request `2362`_
..   * `Ocelot.Discovery.Eureka`_ (renamed from ``Ocelot.Provider.Eureka``, see issue `2371`_) in pull request `2372`_
..   * `Ocelot.QualityOfService.Polly`_ (renamed from ``Ocelot.Provider.Polly``, see issue `2378`_) in pull request `2380`_
..   * `Ocelot.Discovery.Consul`_ (renamed from ``Ocelot.Provider.Consul``, see issue `2378`_) in pull request `2388`_
..   * `Ocelot.Discovery.KubeClient`_ (renamed from ``Ocelot.Provider.Kubernetes``, see issue `2378`_) in pull request `2404`_

..   This keeps the Ocelot core repository lean, avoids delays caused by the integrated packages' own release schedules, and lets each provider evolve independently.
..   Consult the :doc:`../features/caching`, :doc:`../features/tracing`, :doc:`../features/servicediscovery`, and :doc:`../features/kubernetes` chapters for package-specific upgrade notes.
..   Please note that some of the packages are deprecated! To watch the status of each package and its repository, go to the `Release Radar`_ status page.

.. * :doc:`../features/kubernetes`: The ":ref:`PollKube provider <k8s-pollkube-provider>`" was redesigned by `@raman-m`_ in pull request `2358`_.

..   The ``PollKube`` discovery provider was redesigned to utilize ``PeriodicTimer`` (`introduced <https://github.com/dotnet/runtime/pull/53899>`_ in .NET 6).
..   The provider is based on an "active polling" strategy that requires stable behavior and careful management of timing events in multi-threaded scenarios. The new ``PeriodicTimer`` is designed for thread safety, and its callbacks replace the old ``Timer`` callbacks, which work fine in a synchronous flow.
..   Please note that the ``PollKube`` discovery provider is now part of the `Ocelot.Discovery.KubeClient`_ package.

.. * `DevOps`_: Extensive testing and tooling modernization was carried out by contributors ahead of `.NET 10`_:

..   * Solutions upgraded to the `Visual Studio 2026 Solution File Format`_ a.k.a. `.NET 10 CLI solution file format`_ (``.sln`` → ``.slnx``), by `@raman-m`_ in pull request `2354`_.
..   * Testing projects migrated from xUnit v2 to xUnit v3 (by `@methran1304`_ in `2356`_), and acceptance tests migrated to the ``Microsoft.Testing.Platform`` framework (by `@raman-m`_ in `2392`_).
..   * `Ocelot.Testing`_ was re-embedded into `the monorepo`_ (by `@ocelot-ot`_ in `2364`_) and later excluded from Cobertura coverage via ``coverlet.runsettings`` (by `@ocelot-ot`_ in `2365`_).
..   * `Codecov <https://about.codecov.io/>`_ coverage reporting was installed for the repository, by `@raman-m`_ in pull request `2359`_.
..   * NuGet packages were continuously bumped to their latest versions to support .NET SDK ``10.0.*`` (.NET Runtime ``10.0.*``), by `@raman-m`_ and `@ocelot-ot`_ in pull requests `2367`_, `2389`_, `2402`_, and `2409`_.

..   The updated documentation also highlights `the deprecation <https://ocelot.readthedocs.io/en/latest/search.html?q=deprecated>`_ of certain packages through multiple notes and warnings.
..   With the `Obsolete attributes`_ in place, C# developers will notice several warnings in the build logs during compilation.

📦 Patches Included
---------------------
.. _1687: https://github.com/ThreeMammals/Ocelot/issues/1687
.. _2420: https://github.com/ThreeMammals/Ocelot/pull/2420
.. _@rodri-oliveira-dev: https://github.com/rodri-oliveira-dev
.. _RFC 9110: https://www.rfc-editor.org/rfc/rfc9110#section-15.6.5

* :doc:`../features/errorcodes`: Issue `1687`_ was patched by `@rodri-oliveira-dev`_ in pull request `2420`_.

  Ocelot now returns ``504 Gateway Timeout`` when a downstream request times out, in accordance with `RFC 9110`_.
  Client-request cancellation behavior and existing QoS/circuit-breaker behavior remain unchanged.
  Related timeout tests and documentation were updated accordingly.

.. _679: https://github.com/ThreeMammals/Ocelot/issues/679
.. _2414: https://github.com/ThreeMammals/Ocelot/pull/2414
.. _@Ommmarr111: https://github.com/Ommmarr111

* :doc:`../features/authorization`: Issue `679`_ was patched by `@Ommmarr111`_ in pull request `2414`_.

  ``RouteClaimsRequirement`` authorization options now preserve URL-shaped claim type keys such as ``http://schemas.microsoft.com/ws/2008/06/identity/claims/role`` when loaded from configuration.
  ASP.NET Core's binder previously split keys on ``:`` and silently discarded the requirement, causing standard .NET claim types to stop matching routes.
  The fix reconstructs the original keys after binding so authorization policies continue to work correctly.

.. * :doc:`../features/websockets`: Critical issue `2403`_ was patched by `@CurtisRobertOliver`_ in pull request `2406`_.

..   Previously, IP allow/block-list :doc:`../features/routing` ":ref:`Security Options <routing-security-options>`" were not enforced for WebSocket upgrade requests (a.k.a. the ``CONNECT`` HTTP method), allowing them to bypass IP security.
..   WebSocket upgrade requests are now intercepted by the same ``IPSecurityPolicy`` used for regular HTTP requests, and denied upgrades now correctly return ``403 Forbidden``.

..   This critical security issue was `detected by security scanners on GitHub`_ after the bug was reported.
..   Many thanks to George Chen (`@geo-chen`_) for reporting this critical bug.

.. * :doc:`../features/aggregation`: Issue `2248`_ was patched by `@NandanDevHub`_ in pull request `2328`_.

..   :ref:`Complex Aggregation <agg-complex>` routes configured with the ``RouteKeysConfig`` array now correctly map route keys to the corresponding aggregator context, fixing a bug where keys could be mismatched or dropped during multiplexing.

.. * :doc:`../features/administration`: Issue `989`_ was patched by `@mmustafasenoglu`_ in pull request `2412`_.

..   The ``{adminPath}/configuration`` and ``{adminPath}/outputcache/{region}`` endpoints are now decorated with ``[ApiExplorerSettings(IgnoreApi = true)]`` so that they no longer appear in Swagger/OpenAPI documentation generated for the downstream API surface.

🧑‍💻 Contributing
------------------
.. _Pull requests: https://github.com/ThreeMammals/Ocelot/pulls
.. _issues: https://github.com/ThreeMammals/Ocelot/issues
.. _Ocelot GitHub: https://github.com/ThreeMammals/Ocelot/
.. _Ocelot Discussions: https://github.com/ThreeMammals/Ocelot/discussions
.. _ideas: https://github.com/ThreeMammals/Ocelot/discussions/categories/ideas
.. _questions: https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a
.. |octocat| image:: images/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
  :target: https://github.com/ThreeMammals/Ocelot/

`Pull requests`_, `issues`_, and commentary are welcome at the `Ocelot GitHub`_ repository.
For `ideas`_ and `questions`_, please post them in the `Ocelot Discussions`_ space. |octocat|

Our :doc:`../building/devprocess` is a part of successful :doc:`../building/releaseprocess`.
If you are a new contributor, it is crucial to read :doc:`../building/devprocess` attentively to grasp our methods for efficient and swift feature delivery.
We, as a team, advocate adhering to :ref:`dev-best-practices` throughout the development phase.

We extend our best wishes for your successful contributions to the Ocelot product! |octocat|

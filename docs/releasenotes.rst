.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _24.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0-beta.4
.. _25.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0-beta.4
.. _.NET 9: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
.. _.NET 10: https://github.com/ThreeMammals/Ocelot/milestone/13
.. _Globality: https://github.com/ThreeMammals/Ocelot/milestone/9
.. _Ocelot: https://www.nuget.org/packages/Ocelot
.. role::  htm(raw)
    :format: html

.. _welcome:

#######
Welcome
#######

Welcome to the Ocelot `25.0`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major, minor or patched versions.

.. The major version `25.0.0`_ includes several patches, the history of which is outlined below.

.. .. admonition:: Patches

..   - `24.1.1`_, on July 16, 2025: Issue `2299`_ patch ...

.. _release-notes:

Release Notes
-------------
.. _Ocelot.Provider.Kubernetes: https://www.nuget.org/packages/Ocelot.Provider.Kubernetes/
.. _Ocelot.Discovery.KubeClient: https://www.nuget.org/packages/Ocelot.Discovery.KubeClient/
.. _Obsolete attributes: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%5BObsolete&type=code

  | Release Tag: `25.0.0`_
  | Release Codename: `.NET 10`_

In this major release, the Ocelot team focused on preparing the codebase and its ecosystem for `.NET 10`_, while also extracting several integrated extension packages from the mono-repo into their own dedicated repositories to speed up delivery and reduce release coupling.
This release also introduces a built-in :doc:`../features/qualityofservice` (QoS) circuit breaker that works out of the box, without requiring the `Polly`_ library.

The updated documentation highlights `the deprecation <https://ocelot.readthedocs.io/en/latest/search.html?q=deprecated>`_ of certain packages through multiple notes and warnings.
With the `Obsolete attributes`_ in place, C# developers will notice several warnings in the build logs during compilation.

Additionally, this release brings a significant enhancement to the :doc:`../features/kubernetes` ``PollKube`` provider from the `Ocelot.Discovery.KubeClient`_ package.

What's New?
-----------
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
.. _Polly: https://www.nuget.org/packages/Polly/

- :doc:`../features/qualityofservice`: A "`Built-in Circuit Breaker <2384_>`_" implementation was added by `@ocelot-ot`_ in pull request `2385`_, no `Polly`_ required.

  Ocelot's :ref:`qos-schema` no longer strictly depends on the external `Ocelot.QualityOfService.Polly`_ package to provide circuit breaking and timeout enforcement.
  A lightweight, thread-safe circuit breaker (``Closed`` → ``Open`` → ``HalfOpen`` → ``Closed``) ships in the Ocelot core package, supporting both count-based and ``FailureRatio``-based modes.
  The two implementations are mutually exclusive: the last of ``AddQualityOfService()`` or ``AddPolly()`` registered on the ``OcelotBuilder`` wins.
  See the ":ref:`qos-builtin`" documentation for full details, including the ``AddQualityOfService<THandler>()`` extensibility point for overriding server error codes.

- :doc:`../features/websockets`: `Overridable WebSocket buffer size <2386_>`_ and custom middleware injection was added by `@erannevo`_ in pull request `2387`_.

  The previously hard-coded ``DefaultWebSocketBufferSize`` is now a ``protected virtual`` property that can be overridden by subclassing.
  The :doc:`../features/middlewareinjection` feature was extended with a ``WebSocketsProxyMiddleware`` override on ``OcelotPipelineConfiguration``, allowing a fully custom WebSocket middleware to be injected into the pipeline.

- :doc:`../features/websockets`: `SecurityOptions support for the WebSocket pipeline <2403_>`_ was added by `@CurtisRobertOliver`_ in pull request `2406`_.

  Previously, IP allow/block-list :doc:`../features/routing` :ref:`Security Options <routing-security-options>` were not enforced for WebSocket upgrade requests, allowing them to bypass IP security.
  WebSocket upgrade requests are now intercepted by the same ``IPSecurityPolicy`` used for regular HTTP requests, and denied upgrades now correctly return ``403 Forbidden``.

- :doc:`../features/configuration`: "`Merge custom JSON properties across multiple configuration files <651_>`_" was implemented by `@jlukawska`_ in pull request `1183`_.

  Ocelot's configuration builder now merges custom (non-schema) JSON properties defined across multiple ``ocelot.*.json`` files using Newtonsoft's ``JToken`` merge functionality, instead of the last-loaded file silently overwriting properties from previous files.
  This also applies to the ``DynamicRoutes`` collection.
  See the updated :doc:`../features/configuration` documentation, the ":ref:`Extend with Custom Properties <config-custom-properties>`" section and the :ref:`Configuration Sample <config-sample>` app for details.

- :doc:`../features/aggregation`: "`Correct mapping of RouteKeysConfig arrays in aggregates <2248_>`_" fix, contributed in pull request `2328`_.

  Aggregate routes configured with the ``RouteKeysConfig`` array now correctly map route keys to the corresponding aggregator context, fixing a bug where keys could be mismatched or dropped during multiplexing.

What's Updated?
---------------
.. _Ocelot.Cache.CacheManager: https://www.nuget.org/packages/Ocelot.Cache.CacheManager/
.. _Ocelot.Tracing.Butterfly: https://www.nuget.org/packages/Ocelot.Tracing.Butterfly/
.. _Ocelot.Tracing.OpenTracing: https://www.nuget.org/packages/Ocelot.Tracing.OpenTracing/
.. _Ocelot.Discovery.Eureka: https://www.nuget.org/packages/Ocelot.Discovery.Eureka/
.. _Ocelot.QualityOfService.Polly: https://www.nuget.org/packages/Ocelot.QualityOfService.Polly/
.. _Ocelot.Discovery.Consul: https://www.nuget.org/packages/Ocelot.Discovery.Consul/
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
.. _2374: https://github.com/ThreeMammals/Ocelot/issues/2374
.. _2379: https://github.com/ThreeMammals/Ocelot/pull/2379
.. _989: https://github.com/ThreeMammals/Ocelot/issues/989
.. _2412: https://github.com/ThreeMammals/Ocelot/pull/2412
.. _DevOps: https://github.com/ThreeMammals/Ocelot/labels/DevOps
.. _Ocelot.Testing: https://github.com/ThreeMammals/Ocelot.Testing

- Package extraction `2334`_: The Ocelot team, led by `@raman-m`_, continued extracting integrated extension packages out of the mono-repo into their own dedicated repositories, each with an independent release cycle:

  * `Ocelot.Cache.CacheManager`_ — pull request `2360`_
  * `Ocelot.Tracing.Butterfly`_ — pull request `2361`_
  * `Ocelot.Tracing.OpenTracing`_ — pull request `2362`_
  * `Ocelot.Discovery.Eureka`_ (renamed from ``Ocelot.Provider.Eureka``, see issue `2371`_) — pull request `2372`_
  * `Ocelot.QualityOfService.Polly`_ (renamed from ``Ocelot.Provider.Polly``, see issue `2378`_) — pull request `2380`_
  * `Ocelot.Discovery.Consul`_ (renamed from ``Ocelot.Provider.Consul``, see issue `2378`_) — pull request `2388`_
  * `Ocelot.Discovery.KubeClient`_ (renamed from ``Ocelot.Provider.Kubernetes``, see issue `2378`_) — pull request `2404`_

  This keeps the Ocelot core repo lean, avoids delays caused by integrated packages' own release schedules, and lets each provider evolve independently.
  Consult the ":doc:`../features/caching`", ":doc:`../features/tracing`", ":doc:`../features/servicediscovery`" and ":doc:`../features/kubernetes`" chapters for package-specific upgrade notes.

- :doc:`../features/errorcodes`: `Non-ASCII header exceptions mapped to 400 Bad Request <2374_>`_ per RFC 7230, in pull request `2379`_.

  Requests containing non-ASCII characters in HTTP header values previously surfaced as an unhandled ``HttpRequestException`` and an HTTP 500 response.
  Ocelot now catches this ``HttpRequestException`` and maps it to a ``400 Bad Request`` response, in line with RFC 7230 "Field Parsing" rules.

- :doc:`../features/administration`: "`Hide Administration controllers from Swagger and OpenAPI <989_>`_" fix, contributed in pull request `2412`_.

  The ``{adminPath}/configuration`` and ``{adminPath}/outputcache/{region}`` endpoints are now decorated with ``[ApiExplorerSettings(IgnoreApi = true)]`` so that they no longer appear in Swagger/OpenAPI documentation generated for the downstream API surface.

- `DevOps`_: Extensive testing and tooling modernization was carried out by `@raman-m`_ and contributors ahead of `.NET 10`_:

  * Solutions upgraded to the Visual Studio 2026 format.
  * Testing projects migrated from xUnit v2 to xUnit v3, and acceptance tests migrated to the Microsoft.Testing.Platform framework.
  * `Ocelot.Testing`_ was re-embedded into the mono-repo and later excluded from Cobertura coverage via ``coverlet.runsettings``.
  * `Codecov <https://about.codecov.io/>`_ coverage reporting was installed for the repository.
  * All NuGet packages were bumped to their latest versions to support .NET SDK ``10.0.302`` (.NET Runtime ``10.0.10``).

Patches Included
----------------
.. _2346: https://github.com/ThreeMammals/Ocelot/issues/2346
.. _2351: https://github.com/ThreeMammals/Ocelot/pull/2351

- :doc:`../features/routing`: Issue `2346`_ patch, contributed in pull request `2351`_.

  This fixes a bug where downstream URL query parameter names could be corrupted if they contained a placeholder with a non-empty value.
  The ``DownstreamUrlCreatorMiddleware`` query string merging algorithm was refactored to take full advantage of ASP.NET Core's ``QueryHelpers``.


Contributing
------------

.. |octocat| image:: images/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
  :target: https://github.com/ThreeMammals/Ocelot/
.. _Pull requests: https://github.com/ThreeMammals/Ocelot/pulls
.. _issues: https://github.com/ThreeMammals/Ocelot/issues
.. _Ocelot GitHub: https://github.com/ThreeMammals/Ocelot/
.. _Ocelot Discussions: https://github.com/ThreeMammals/Ocelot/discussions
.. _ideas: https://github.com/ThreeMammals/Ocelot/discussions/categories/ideas
.. _questions: https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a

`Pull requests`_, `issues`_, and commentary are welcome at the `Ocelot GitHub`_ repository.
For `ideas`_ and `questions`_, please post them in the `Ocelot Discussions`_ space. |octocat|

Our :doc:`../building/devprocess` is a part of successful :doc:`../building/releaseprocess`.
If you are a new contributor, it is crucial to read :doc:`../building/devprocess` attentively to grasp our methods for efficient and swift feature delivery.
We, as a team, advocate adhering to :ref:`dev-best-practices` throughout the development phase.

We extend our best wishes for your successful contributions to the Ocelot product! |octocat|

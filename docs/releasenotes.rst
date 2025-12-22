.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _24.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
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

Welcome to the Ocelot `24.1`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major, minor or patched versions.

.. The major version `24.1.0`_ includes several patches, the history of which is outlined below.

.. .. admonition:: Patches

..   - `24.1.1`_, on July 16, 2025: Issue `2299`_ patch ...

.. _release-notes:

Release Notes
-------------
.. _Ocelot.Provider.Kubernetes: https://www.nuget.org/packages/Ocelot.Provider.Kubernetes/
.. _Obsolete attributes: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%5BObsolete&type=code

  | Release Tag: `24.1.0`_
  | Release Codename: `Globality`_

In this minor release, the Ocelot team put the spotlight on the :doc:`../features/configuration` feature as part of their semi-annual 2025 effort, with a particular focus on the :ref:`config-global-configuration-schema`.
This release enhances support for global configurations across both routing modes: the classic static :doc:`../features/routing` and the :doc:`service discovery <../features/servicediscovery>`-based :ref:`Dynamic Routing <sd-dynamic-routing>`.

The updated documentation highlights `the deprecation <https://ocelot.readthedocs.io/en/develop/search.html?q=deprecated>`_ of certain options through multiple notes and warnings.
This deprecation process will be completed in the upcoming `.NET 10`_ release.
With the `Obsolete attributes`_ in place, C# developers will notice several warnings in the build logs during compilation.

On top of that, this release brings a great enhancement to the :doc:`../features/kubernetes` provider, also known as the `Ocelot.Provider.Kubernetes`_ package.

What's New?
-----------
.. _@raman-m: https://github.com/raman-m
.. _@kick2nick: https://github.com/kick2nick
.. _@hogwartsdeveloper: https://github.com/hogwartsdeveloper
.. _@RaynaldM: https://github.com/RaynaldM
.. _585: https://github.com/ThreeMammals/Ocelot/issues/585
.. _2073: https://github.com/ThreeMammals/Ocelot/pull/2073
.. _2081: https://github.com/ThreeMammals/Ocelot/pull/2081
.. _2174: https://github.com/ThreeMammals/Ocelot/pull/2174
.. _Dynamic routing global configuration: https://github.com/ThreeMammals/Ocelot/issues/585
.. _KubeClient: https://www.nuget.org/packages/KubeClient/
.. _Polly: https://www.nuget.org/packages/Polly/
.. _Ocelot.Provider.Polly: https://www.nuget.org/packages/Ocelot.Provider.Polly
.. _FailureRatio and SamplingDuration parameters of Polly V8 circuit-breaker: https://github.com/ThreeMammals/Ocelot/issues/2080

- :doc:`../features/configuration`: The "`Dynamic routing global configuration`_" feature has been redesigned by `@raman-m`_ and contributors.

  This update brings changes to the :ref:`config-dynamic-route-schema` and :ref:`config-global-configuration-schema`, while the :ref:`config-route-schema` stays the same apart from deprecation updates.
  All work was coordinated under issue `585`_, which addressed the challenges of configuring Ocelot's most popular features globally before version `24.1`_, when :ref:`dynamic routing <sd-dynamic-routing>` gained global configuration partial support, but static routing mostly lacked it.
  A key outcome of `585`_ is the ability to override global configuration options within the ``DynamicRoutes`` collection.
  This ongoing issue will continue to require attention, as adapting static route global configurations for :ref:`dynamic routing <sd-dynamic-routing>` is complex and, in some cases, impossible.
  This will be a challenge for future `Ocelot`_ releases and the community.

- :doc:`../features/kubernetes`: The ":ref:`Kubernetes provider based on watch requests <k8s-watchkube-provider>`" feature by `@kick2nick`_ in pull request `2174`_.

  The `Ocelot.Provider.Kubernetes`_ package now features a new :ref:`WatchKube provider <k8s-watchkube-provider>` for :doc:`Kubernetes <../features/kubernetes>` service discovery.
  This provider is a great fit for high-load environments where the older :ref:`Kube <k8s-kube-provider>` and :ref:`PollKube <k8s-pollkube-provider>` providers struggle to handle heavy traffic, often leading to increased log errors, HTTP 500 issues, and potential Ocelot instance failures.
  ``WatchKube`` is the next step in the evolution of these providers, leveraging the reactive capabilities of the `KubeClient`_ API.
  For guidance on choosing the right provider for your Kubernetes setup, check out the ":ref:`k8s-comparing-providers`" section.

- :doc:`../features/configuration`: The ":ref:`Routing default timeout <config-timeout>`" feature by `@hogwartsdeveloper`_ in pull request `2073`_.

  In the past, the ``Timeout`` setting in the :ref:`config-route-schema` did not actually stop requests, defaulting instead to a fixed `90 seconds <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%2290+seconds%22&type=code>`_.
  Custom timeouts were handled using the :doc:`../features/qualityofservice` :ref:`qos-timeout-strategy`, and this only applied if `Polly`_ and the `Ocelot.Provider.Polly`_ package were used.
  Now, the ``Timeout`` option (in seconds) can be set at the route, global, and QoS levels.
  The :ref:`config-global-configuration-schema` and :ref:`config-dynamic-route-schema` also include the new ``Timeout`` setting, making it possible to configure default timeouts for :ref:`dynamic routing <sd-dynamic-routing>` as well.

- :doc:`../features/qualityofservice`: The "`FailureRatio and SamplingDuration parameters of Polly V8 circuit-breaker`_" feature by `@RaynaldM`_ in pull request `2081`_.

  Starting with version `24.1`_, two new options in :ref:`qos-schema`, ``FailureRatio`` and ``SamplingDuration``, let you fine-tune the behavior of the :ref:`qos-circuit-breaker-strategy`.
  Both can be :ref:`configured globally <qos-global-configuration>`, even with :ref:`dynamic routing <sd-dynamic-routing>`.

  .. note:: The ``DurationOfBreak``, ``ExceptionsAllowedBeforeBreaking``, and ``TimeoutValue`` options are now deprecated in `24.1`_, so check the ":ref:`qos-schema`" documentation for details.

What's Updated?
---------------
.. _@marklonquist: https://github.com/marklonquist
.. _@jlukawska: https://github.com/jlukawska
.. _@MiladRv: https://github.com/MiladRv
.. _1592: https://github.com/ThreeMammals/Ocelot/pull/1592
.. _1659: https://github.com/ThreeMammals/Ocelot/pull/1659
.. _2114: https://github.com/ThreeMammals/Ocelot/pull/2114
.. _2294: https://github.com/ThreeMammals/Ocelot/pull/2294
.. _2295: https://github.com/ThreeMammals/Ocelot/pull/2295
.. _2324: https://github.com/ThreeMammals/Ocelot/pull/2324
.. _2331: https://github.com/ThreeMammals/Ocelot/pull/2331
.. _2332: https://github.com/ThreeMammals/Ocelot/pull/2332
.. _2336: https://github.com/ThreeMammals/Ocelot/pull/2336
.. _2339: https://github.com/ThreeMammals/Ocelot/pull/2339
.. _2342: https://github.com/ThreeMammals/Ocelot/pull/2342
.. _2345: https://github.com/ThreeMammals/Ocelot/pull/2345
.. _2347: https://github.com/ThreeMammals/Ocelot/pull/2347
.. _File-model: https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot/Configuration/File
.. _deprecated options: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+deprecated+language%3AreStructuredText&type=code&l=reStructuredText
.. _Ocelot.Testing: https://github.com/ThreeMammals/Ocelot/tree/24.0.0/test/Ocelot.Testing
.. _extension packages: https://www.nuget.org/profiles/ThreeMammals
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _DevOps: https://github.com/ThreeMammals/Ocelot/labels/DevOps
.. _GH-Actions: https://github.com/ThreeMammals/Ocelot/actions

- :doc:`../features/configuration`: Several `File-model`_ options have been deprecated by `@raman-m`_.

  The updated docs now highlight these `deprecated options`_ with multiple notes and warnings.
  The `24.1`_ deprecation process will wrap up in the upcoming `.NET 10`_ release.
  Due to the `Obsolete attributes`_, C# developers will notice several build warnings during compilation.

- :ref:`b-testing`: The `Ocelot.Testing`_ project was deprecated by `@raman-m`_ in pull request `2295`_.

  The project was removed from the main repo and moved to its own `Ocelot.Testing <https://github.com/ThreeMammals/Ocelot.Testing>`__ repository.
  This change allows the `Ocelot.Testing <https://www.nuget.org/packages/Ocelot.Testing/>`__ package to be shared independently for delivery of `extension packages`_.
  The Ocelot team also plans to deprecate more projects and move them to separate repos because:
  **a)** despite the fact that a monorepo enables faster builds and quicker delivery;
  **b)** but the release process can be delayed by missing versions of integrated libraries in `extension packages`_.
  The goal is for the Ocelot repo to only contain essential projects, avoiding delays caused by integrated package release schedules.
  Legacy or abandoned integrated packages should be deprecated and maintained in their own repos with independent release cycles.

- :doc:`../features/headerstransformation`: Added :ref:`global configuration <ht-configuration>` by `@marklonquist`_ in pull request `1659`_.

  The :ref:`config-global-configuration-schema` now includes new ``DownstreamHeaderTransform`` and ``UpstreamHeaderTransform`` options.
  These work only with static routes, meaning the ``Routes`` collection (see :ref:`config-route-schema`).
  They are not supported for dynamic routes because they are not part of the :ref:`config-dynamic-route-schema`, and Ocelot Core does not read global configuration of this feature in :ref:`dynamic routing <sd-dynamic-routing>` mode.
  This is noted in the :ref:`ht-roadmap` documentation.

- :doc:`../features/authentication`: Added :ref:`global configuration <authentication-configuration>` by `@jlukawska`_ in pull request `2114`_.

  The :ref:`config-global-configuration-schema` now includes a new ``AuthenticationOptions`` property for setting up static routes globally.
  This also introduces the :ref:`AllowAnonymous boolean option <authentication-configuration>` within ``AuthenticationOptions`` to control static route authentication.
  Later, pull request `2336`_ extended global authentication support to dynamic routes.

  .. note:: The ``AuthenticationProviderKey`` option is deprecated in version `24.1`_—see the ":ref:`authentication-options-schema`" documentation for details.

- :doc:`../features/ratelimiting`: Re-designed :ref:`global configuration <rl-configuration>` by `@MiladRv`_ and `@raman-m`_ in pull request `2294`_.

  The :ref:`config-global-configuration-schema` now includes a new ``RateLimitOptions`` property for both static and dynamic routes.
  Previously, global configuration was available through ``RateLimitOptions`` in :ref:`dynamic routing <sd-dynamic-routing>` mode, while route overriding used the now-deprecated ``RateLimitRule`` from the :ref:`config-dynamic-route-schema`.

  This marks the second major overhaul of the *Rate Limiting* feature since the first update in pull request `1592`_.
  A new ``Wait`` option has been added, replacing the deprecated ``PeriodTimespan``, to enhance the :ref:`Fixed Window <rl-algorithms>` algorithm.
  The full list of deprecated options can be found in the ":ref:`Deprecated options <rl-deprecated-options>`" documentation.

- :doc:`../features/loadbalancer`: Added :ref:`global configuration <lb-global-configuration>` by `@raman-m`_ in pull request `2324`_.

  The :ref:`config-global-configuration-schema` now includes a new ``LoadBalancerOptions`` property for both static and dynamic routes.
  Previously, global configuration was available through ``LoadBalancerOptions`` in :ref:`dynamic routing <sd-dynamic-routing>` mode without dynamic route overrides.
  Starting with version `24.1`_, the :ref:`config-dynamic-route-schema` also supports ``LoadBalancerOptions`` for overriding, and global configuration for static routes is now supported as well.

- :doc:`../features/caching`: Added :ref:`global configuration <caching-global-configuration>` by `@raman-m`_ in pull request `2331`_.

  The :ref:`config-global-configuration-schema` now includes a new ``CacheOptions`` property for both static and dynamic routes.
  Global configuration has been available for static routes since version `23.3`_, but starting with version `24.1`_, the :ref:`config-dynamic-route-schema` also supports ``CacheOptions`` for overriding.

  .. note::
    The ``FileCacheOptions`` property in the :ref:`config-route-schema` (static routes) is deprecated in version `24.1`_.
    For more details, see the caching :ref:`caching-configuration` documentation.

- :ref:`Http Handler <config-http-handler-options>`: Added :ref:`global configuration <config-http-handler-options>` by `@raman-m`_ in pull request `2332`_.

  The :ref:`config-global-configuration-schema` now includes a new ``HttpHandlerOptions`` property for both static and dynamic routes.
  Previously, global configuration was available through ``HttpHandlerOptions`` in :ref:`dynamic routing <sd-dynamic-routing>` mode without dynamic route overriding.
  Starting with version `24.1`_, the :ref:`config-dynamic-route-schema` also supports ``HttpHandlerOptions`` for overriding, and global configuration is now available for static routes as well.

- :doc:`../features/authentication`: Added :ref:`global configuration <authentication-global-configuration>` by `@raman-m`_ in pull request `2336`_.

  The :ref:`config-global-configuration-schema` now includes a new ``AuthenticationOptions`` property for both static and dynamic routes.
  Starting with version `24.1`_, the :ref:`config-dynamic-route-schema` also supports ``AuthenticationOptions`` to override global settings.

  .. note::
    The ``AuthenticationProviderKey`` option is deprecated in version `24.1`_, so check the ":ref:`authentication-options-schema`" documentation for details.

- :doc:`../features/qualityofservice`: Added :ref:`global configuration <qos-global-configuration>` by `@raman-m`_ in pull request `2339`_.

  The :ref:`config-global-configuration-schema` now includes a new ``QoSOptions`` property for both static and dynamic routes.
  Previously, global configuration was available through ``QoSOptions`` in :ref:`dynamic routing <sd-dynamic-routing>` mode without the option for dynamic route overrides.
  Starting with version `24.1`_, the :ref:`config-dynamic-route-schema` supports ``QoSOptions`` for overriding, and global configuration support is now available for static routes as well.

  .. note::
    The ``DurationOfBreak``, ``ExceptionsAllowedBeforeBreaking``, and ``TimeoutValue`` options are deprecated in version `24.1`_.
    For details, see the ":ref:`qos-schema`" documentation.

- `DevOps`_: Stabilized tests and reviewed `GH-Actions`_ workflows by `@raman-m`_ in pull requests `2342`_ and `2345`_.

  These efforts kept the CI/CD builds in `GitHub Actions <https://github.com/ThreeMammals/Ocelot/actions>`_ stable, targeting the `alpha release <https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0-pre-release-24-1.1>`_ of version `24.1`_.
  The CI/CD environment was set up and tested `GH-Actions`_ workflows in advance for the beta release, which is the goal of pull request `2347`_.

Patches Included
----------------
.. _@mehyaa: https://github.com/mehyaa
.. _913: https://github.com/ThreeMammals/Ocelot/issues/913
.. _930: https://github.com/ThreeMammals/Ocelot/issues/930
.. _1478: https://github.com/ThreeMammals/Ocelot/pull/1478
.. _2091: https://github.com/ThreeMammals/Ocelot/pull/2091
.. _2304: https://github.com/ThreeMammals/Ocelot/issues/2304
.. _2335: https://github.com/ThreeMammals/Ocelot/pull/2335
.. _RFC 8693: https://datatracker.ietf.org/doc/html/rfc8693

- :doc:`../features/websockets`: Issue `930`_ patch by `@hogwartsdeveloper`_ in pull request `2091`_.

  This update removes the troublesome ``System.Net.WebSockets.WebSocketException`` from logs, preventing Ocelot from running into 500 status disasters.
  The issue stemmed from client-side or network events that Ocelot's ``WebSocketsProxyMiddleware`` could not anticipate on the server side.
  The patch now checks for incorrect connection statuses, attempting to close the connection and end server-side tasks gracefully without errors.

- :doc:`../features/kubernetes`: Issue `2304`_ patch by `@raman-m`_ in pull request `2335`_.

  This update fixes the :ref:`PollKube provider <k8s-pollkube-provider>` to address a bug with the first cold request, where the winning thread got an empty collection before the initial callback was triggered.
  The solution is to call the integrated discovery provider for the first cold request when the queue is empty.

- :doc:`../features/authorization`: Issue `913`_ patch by `@mehyaa`_ in pull request `1478`_.

  Starting with version `24.1`_, Ocelot now supports `RFC 8693`_ (OAuth 2.0 Token Exchange) for the '``scope``' claim in the ``ScopesAuthorizer`` service, also referred to as the ``IScopesAuthorizer`` service in the DI container.
  This is noted in the ":ref:`authentication-allowed-scopes`" documentation (see the first note).

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

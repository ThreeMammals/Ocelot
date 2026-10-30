.. role:: htm(raw)
    :format: html

.. _welcome:

#######
Welcome
#######
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
.. _25.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
.. _25.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.1
.. _25.0.2: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.2
.. _25.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
.. _10.0.401: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
.. _10.0.402: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
.. _2352: https://github.com/ThreeMammals/Ocelot/issues/2352
.. _2428: https://github.com/ThreeMammals/Ocelot/issues/2428

.. Welcome to the Ocelot `25.0`_ documentation!

.. warning::
  | Version `25.1 <https://github.com/ThreeMammals/Ocelot/milestone/12>`_ is in development! Full changelog: `release/25.0...develop <https://github.com/ThreeMammals/Ocelot/compare/release/25.0...develop>`_
  | Read the latest **25.0** documentation here: `v25.0 <https://ocelot.readthedocs.io/en/25.0/>`_

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major, minor or patched versions.

.. The major version `25.0.0`_ includes several patches, the history of which is outlined below.

.. .. admonition:: Patches

..   - `25.0.1`_, on September 14, 2026: Reference SDK `10.0.401`_ (Runtime `10.0.12 <https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md>`_)
..   - `25.0.2`_, on October 15, 2026: Reference SDK `10.0.402`_ (Runtime `10.0.13 <https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md>`_).
..     Hotfix for bugs `2352`_ and `2428`_, a :ref:`Dynamic Routing <sd-dynamic-routing>` patch.

.. _release-notes:

📢 Release Notes
-----------------

  | Release Tag: `25.1.0`_ (to be created)
  | Release Codename: `Rise of Ocelot <https://github.com/ThreeMammals/Ocelot/milestone/12>`_
  | Release Date: ... 2026

To be added...

🆕 What's New?
----------------
.. _@Sacrilege: https://github.com/Sacrilege
.. _1521: https://github.com/ThreeMammals/Ocelot/pull/1521
.. _WebClient.UseDefaultCredentials: https://learn.microsoft.com/en-us/dotnet/api/system.net.webclient.usedefaultcredentials
.. _SocketsHttpHandler.Credentials: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.credentials

* :ref:`HTTP Handler <config-http-handler-options>`: The *"Pass-through Credentials"* feature was added by `@Sacrilege`_ in pull request `1521`_.

  This feature is the result of updates to three Ocelot features: :doc:`../features/configuration`, :doc:`../features/authentication`, and :doc:`../features/authorization`.
  Note that the *HTTP Handler*, a :class:`SocketsHttpHandler` object, is created by :class:`MessageInvokerPool`.

  * :doc:`../features/configuration`: The :ref:`config-http-handler-options` schema was extended for downstream credential handling.

    The configuration model now exposes ``UseCredentials`` for route-level *HTTP handler* setup, allowing Ocelot to initialize ``SocketsHttpHandler.Credentials`` with the default network credentials used by the current execution context.
    This complements the :doc:`../features/authentication` and :doc:`../features/authorization` features by enabling Windows-authenticated downstream calls to be configured per route without changing the broader gateway behavior.

  * :doc:`../features/authentication`: The ":ref:`Pass Through Authentication <authentication-pass-through>`" feature.

    When :ref:`config-http-handler-options` ``UseCredentials`` is enabled for a route, Ocelot initializes ``SocketsHttpHandler.Credentials`` to use default credentials for the outbound HTTP request.
    This is especially useful for Windows Integrated Authentication scenarios, including NTLM and Kerberos, where the downstream service or proxy expects the caller's identity to be propagated without explicitly setting credentials in code.
    The setting is scoped to the route, which keeps authentication behavior predictable and avoids forcing a global credential policy across every downstream service.
    For .NET developers, this is the same general pattern described by `WebClient.UseDefaultCredentials`_, but applied at the Ocelot :ref:`HTTP Handler <config-http-handler-options>` level for downstream requests.

  * :doc:`../features/authorization`: Route-level credential configuration was aligned with authorization scenarios through the ":ref:`authorization-pass-through`" feature.

    Authorization rules remain claim-based and route-scoped, while ``UseCredentials`` controls how the downstream HTTP client authenticates to the target service.
    This allows Windows-authenticated routes to coexist with existing route policy checks and claim requirements without altering the authorization model itself.

.. _1389: https://github.com/ThreeMammals/Ocelot/pull/1389
.. _1741: https://github.com/ThreeMammals/Ocelot/discussions/1741
.. _@crb02005: https://github.com/crb02005

* :doc:`../features/aggregation`: The ":ref:`Upstream HTTP Methods <agg-upstream-http-methods>`" feature was added by `@crb02005`_ in pull request `1389`_.

  Aggregate routes can now accept HTTP methods other than ``GET`` and proxy/forward request bodies to downstream services.
  The ``UpstreamHttpMethod`` option is available per aggregate route and globally in ``GlobalConfiguration``, and the two lists are merged.
  The default method, used as a fallback when JSON configuration is absent, can also be customized in C# by overriding the ``DefaultHttpMethod`` property of :class:`AggregatesCreator`.

{ features in development } 

🆙 What's Updated?
--------------------

{ updates in development } 

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

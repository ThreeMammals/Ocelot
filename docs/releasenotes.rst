.. _Polly: https://github.com/App-vNext/Polly
.. _Circuit Breaker: https://www.pollydocs.org/strategies/circuit-breaker.html
.. _Timeout: https://www.pollydocs.org/strategies/timeout.html

.. _@raman-m: https://github.com/raman-m
.. _@RaynaldM: https://github.com/RaynaldM
.. _@jlukawska: https://github.com/jlukawska
.. _@ibnuda: https://github.com/ibnuda
.. _@vantm: https://github.com/vantm
.. _@sergio-str: https://github.com/sergio-str
.. _@PaulARoy: https://github.com/PaulARoy
.. _@thiagoloureiro: https://github.com/thiagoloureiro
.. _@bbenameur: https://github.com/bbenameur

.. _23.2.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _23.3.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _23.3.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.3
.. _23.3.4: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4
.. _23.3.5: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.5
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4

.. _954: https://github.com/ThreeMammals/Ocelot/issues/954
.. _957: https://github.com/ThreeMammals/Ocelot/issues/957
.. _1026: https://github.com/ThreeMammals/Ocelot/issues/1026
.. _1312: https://github.com/ThreeMammals/Ocelot/pull/1312
.. _1590: https://github.com/ThreeMammals/Ocelot/issues/1590
.. _1592: https://github.com/ThreeMammals/Ocelot/pull/1592
.. _1673: https://github.com/ThreeMammals/Ocelot/pull/1673
.. _1843: https://github.com/ThreeMammals/Ocelot/pull/1843
.. _2002: https://github.com/ThreeMammals/Ocelot/issues/2002
.. _2003: https://github.com/ThreeMammals/Ocelot/pull/2003
.. _2034: https://github.com/ThreeMammals/Ocelot/issues/2034
.. _2039: https://github.com/ThreeMammals/Ocelot/issues/2039
.. _2045: https://github.com/ThreeMammals/Ocelot/pull/2045
.. _2050: https://github.com/ThreeMammals/Ocelot/pull/2050
.. _2052: https://github.com/ThreeMammals/Ocelot/pull/2052
.. _2054: https://github.com/ThreeMammals/Ocelot/discussions/2054
.. _2058: https://github.com/ThreeMammals/Ocelot/pull/2058
.. _2059: https://github.com/ThreeMammals/Ocelot/issues/2059
.. _2067: https://github.com/ThreeMammals/Ocelot/pull/2067
.. _2079: https://github.com/ThreeMammals/Ocelot/pull/2079
.. _2085: https://github.com/ThreeMammals/Ocelot/issues/2085
.. _2086: https://github.com/ThreeMammals/Ocelot/pull/2086

.. role::  htm(raw)
    :format: html

.. _welcome:

#######
Welcome
#######

Welcome to the Ocelot `23.3`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major or patched versions.

The major version `23.3.0`_ includes several patches, the history of which is provided below.

.. admonition:: Patches

  - `23.3.3`_, on Jun 11, 2024. Technical release with DevOps patch.
  - `23.3.4`_, on Oct 3, 2024. Hot fixing version `23.3.0`_, codenamed `Blue Olympic Balumbes <https://www.youtube.com/live/j-Ou-ggS718?si=fPPwmOwjYEZq70H9&t=9518>`_ release,
    with codename decoding links:

    - **for men**: naked `Blue Olympic Fiend <https://www.youtube.com/live/j-Ou-ggS718?si=fPPwmOwjYEZq70H9&t=9518>`_ 
    - **for women**: `not a well-dressed woman <https://www.youtube.com/live/j-Ou-ggS718?si=fPPwmOwjYEZq70H9&t=9518>`_ sings at the opening ceremony, so "Not `Celine Dion <https://www.celinedion.com/>`_" 
    - **for black men**: enjoy `Men's Basketball Final <https://www.youtube.com/watch?v=Xci7dzk-bFk>`_ in `Paris 2024 <https://www.youtube.com/hashtag/paris2024>`_.
      Be proud of Stephen Curry, "just give me a ball" boy, as an absolute rockstar, made `shot 1 <https://www.youtube.com/watch?v=Xci7dzk-bFk&t=832s>`_, `shot 2 <https://www.youtube.com/watch?v=Xci7dzk-bFk&t=1052s>`_, `shot 3 <https://www.youtube.com/watch?v=Xci7dzk-bFk&t=1087s>`_  and final `shot 4 <https://www.youtube.com/watch?v=Xci7dzk-bFk&t=1216s>`_.

  - `23.3.5`_, on Oct 12, 2024. Documentation patch: Html and Pdf doc layouts.

.. _release-notes:

Release Notes
-------------

| Release Tag: `23.3.0`_
| Release Codename: `Twilight Texas <https://www.timeanddate.com/eclipse/solar/2024-april-8>`_, with codename decoding links:
  `for men <https://www.timeanddate.com/eclipse/map/2024-april-8>`_,
  `for women <https://www.goodreads.com/series/50439-twilight-texas>`_,
  `for black men <https://rollingout.com/2024/06/03/eclipse-darkness-busta-rhymes-twista/>`_.

What's new?
^^^^^^^^^^^

- :doc:`../features/servicediscovery`: Introducing a new feature for "*Customization of services creation*" in two primary service discovery providers: ``Consul`` (:ref:`sd-consul-service-builder`) and ``Kubernetes`` (:ref:`k8s-downstream-scheme-vs-port-names`), developed by `@raman-m`_.

  The customization for both ``Consul`` and ``Kube`` providers in service creation is achieved through the overriding of virtual methods in default implementations. The recommendation was to separate the provider's logic and introduce ``public virtual`` and ``protected virtual`` methods in concrete classes, enabling:

  - The use of ``public virtual`` methods as dictated by interface definitions.
  - The application of ``protected virtual`` methods to allow developers to customize atomic operations through inheritance from existing concrete classes.
  - The injection of new interface objects into the provider's constructor.
  - The overriding of the default behavior of classes.

  | Ultimately, customization relies on the virtual methods within the default implementation classes, providing developers the flexibility to override them as necessary for highly tailored Consul/K8s configurations in their specific environments.
  | For further details, refer to the respective pull requests for both providers: ``Kube`` (PR `2052`_), ``Consul`` (PR `2067`_).

- :doc:`../features/routing`: Introducing the new ":ref:`routing-upstream-headers`" feature by `@jlukawska`_.

  | In addition to routing via ``UpstreamPathTemplate``, you can now define an ``UpstreamHeaderTemplates`` options dictionary. For a route to match, all headers specified in this section are required to be present in the request headers.
  | For more details, see PR `1312`_.

- :doc:`../features/configuration`: Introducing the ":ref:`config-version-policy`" feature by `@ibnuda`_.

  The configurable ``HttpRequestMessage.VersionPolicy`` helps avoid HTTP protocol connection errors and stabilizes connections to downstream services, especially when you're not developing those services, documentation is scarce, or the deployed HTTP protocol version is uncertain.
  For developers of downstream services, it's possible to ``ConfigureKestrel`` server and its endpoints with new protocol settings. However, attention to version policy is also required, and this feature provides precise version settings for HTTP connections.

  | Essentially, this feature promotes the use of HTTP protocols beyond 1.0/1.1, such as HTTP/2 or even HTTP/3.
  | For additional details, refer to PR `1673`_.

- :doc:`../features/configuration`: Introducing the new ":ref:`config-route-metadata`" feature by `@vantm`_.

  Undoubtedly, this is the standout feature of the release! ⭐

  Route metadata enables Ocelot developers to incorporate custom functions that address specific needs or to create their own plugins/extensions.

  In versions of Ocelot prior to `23.3.0`_, the configuration was limited to predefined values that Ocelot used internally. This was sufficient for official extensions, but posed challenges for third-party developers who needed to implement configurations not included in the standard ``FileConfiguration``.
  Applying an option to a specific route required knowledge of the array index and other details that might not be readily accessible using the standard ``IConfiguration`` or ``IOptions<FileConfiguration>`` models from ASP.NET.

  | Now, :doc:`../features/metadata` can be directly accessed in the ``DownstreamRoute`` object. Furthermore, metadata can also be retrieved from the global JSON section via the ``FileConfiguration.GlobalConfiguration`` property.
  | For more information, see the details in PR `1843`_ on this remarkable feature.

Updates of the features
^^^^^^^^^^^^^^^^^^^^^^^

- :doc:`../features/configuration`: New features are ":ref:`config-version-policy`" by `@ibnuda`_ and ":ref:`config-route-metadata`" by `@vantm`_.
- :doc:`../features/servicediscovery`: New feature is "*Customization of services creation*" aka :ref:`sd-consul-service-builder` and :ref:`k8s-downstream-scheme-vs-port-names` by `@raman-m`_.
- :doc:`../features/routing`: New feature is ":ref:`routing-upstream-headers`" by `@jlukawska`_.
- :doc:`../features/qualityofservice`: The team has decided to remove the Polly V7 policies logic and the corresponding Ocelot ``AddPollyV7`` extensions (referenced in PR `2079`_).

  | Furthermore, the Polly V8 Circuit Breaker has been mandated as the primary strategy (as per PR `2086`_).
  | See more detaild below in "**Ocelot extra packages**" paragraph.

Ocelot extra packages
^^^^^^^^^^^^^^^^^^^^^

- `Ocelot.Provider.Polly <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_

  - Our team has resolved to eliminate the Polly V7 policies logic and the corresponding Ocelot ``AddPollyV7`` extensions entirely (refer to the "`Polly v7 vs v8 <https://ocelot.readthedocs.io/en/23.2.2/features/qualityofservice.html#polly-v7-vs-v8>`_" documentation).
    In the previous `23.2.0`_ release, named `Lunar Eclipse <https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0>`_, we included these to maintain the legacy `Polly`_ behavior, allowing development teams to transition or retain the old Polly V7 functionality.
    We are now confident that it is time to progress alongside `Polly`_, shifting our focus to the new `Polly V8 <https://www.thepollyproject.org/2023/09/28/polly-v8-officially-released/>`_ `resilience pipelines <https://www.pollydocs.org/pipelines/>`_.
    For more details, see PR `2079`_.
  - Additionally, we have implemented Polly v8 `Circuit Breaker <https://www.pollydocs.org/strategies/circuit-breaker.html>`_ as the primary strategy.
    Our :doc:`../features/qualityofservice` (QoS) relies on two main strategies: :ref:`qos-circuit-breaker-strategy` and :ref:`qos-timeout-strategy`.
    If both `Circuit Breaker`_ and `Timeout`_ have :ref:`qos-configuration` with their respective properties in the ``QoSOptions`` of the route JSON, then the :ref:`qos-circuit-breaker-strategy` will take precedence in the constructed resilience pipeline.
    For more details, refer to PR `2086`_.

Stabilization (bug fixing)
^^^^^^^^^^^^^^^^^^^^^^^^^^

- Fixed `2034`_ in PR `2045`_ by `@raman-m`_
- Fixed `2039`_ in PR `2050`_ by `@PaulARoy`_
- Fixed `1590`_ in PR `1592`_ by `@sergio-str`_
- Fixed `2054`_ `2059`_ in PR `2058`_ by `@thiagoloureiro`_
- Fixed `954`_ `957`_ `1026`_ in PR `2067`_ by `@raman-m`_
- Fixed `2002`_ in PR `2003`_ by `@bbenameur`_
- Fixed `2085`_ in PR `2086`_ by `@RaynaldM`_

See `all bugs <https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+milestone%3ASpring%2724+is%3Aclosed+label%3Abug>`_ of the `Spring'24 <https://github.com/ThreeMammals/Ocelot/milestone/6>`_ milestone

Documentation Summary
^^^^^^^^^^^^^^^^^^^^^

- :doc:`../features/caching`: New :ref:`cch-enablecontenthashing-option` and :ref:`cch-global-configuration` sections
- :doc:`../features/configuration`: New :ref:`config-version-policy` and :ref:`config-route-metadata` sections
- :doc:`../features/kubernetes`: New :ref:`k8s-downstream-scheme-vs-port-names` section
- :doc:`../features/metadata`: This is new chapter for :ref:`config-route-metadata` feature
- :doc:`../features/qualityofservice`
- :doc:`../features/ratelimiting`
- :doc:`../features/requestaggregation`
- :doc:`../features/routing`: New :ref:`routing-upstream-headers` section
- :doc:`../features/servicediscovery`: New :ref:`sd-consul-service-builder` and :ref:`k8s-downstream-scheme-vs-port-names` sections

Contributing
------------

`Pull requests <https://github.com/ThreeMammals/Ocelot/pulls>`_, `issues <https://github.com/ThreeMammals/Ocelot/issues>`_, and commentary are welcome at the `Ocelot GitHub repository <https://github.com/ThreeMammals/Ocelot/>`_.

For `ideas <https://github.com/ThreeMammals/Ocelot/discussions/categories/ideas>`_ and `questions <https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a>`_, please post them in the `Ocelot Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space.

Our development process is detailed in the :doc:`../building/releaseprocess` documentation.

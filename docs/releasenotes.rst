.. role::  htm(raw)
    :format: html
.. _Polly: https://github.com/App-vNext/Polly
.. _@ebjornset: https://github.com/ebjornset
.. _@RaynaldM: https://github.com/RaynaldM
.. _@ArwynFr: https://github.com/ArwynFr
.. _@AlyHKafoury: https://github.com/AlyHKafoury
.. _@FelixBoers: https://github.com/FelixBoers
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _23.2.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _23.2.1: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.1
.. _23.2.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.2
.. _2031: https://github.com/ThreeMammals/Ocelot/issues/2031

.. _welcome:

#######
Welcome
#######

Welcome to the Ocelot `23.2`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major or patched versions.

.. _release-notes:

Release Notes
-------------

  | Release Tag: `23.2.0`_
  | Release Codename: `Lunar Eclipse <https://www.timeanddate.com/eclipse/lunar/2024-march-25>`_
  | Release Date: April 1, 2024

The major version `23.2.0`_ includes several patches, the history of which is outlined below.

.. admonition:: Patches

  - `23.2.1`_, on April 3, 2024: Documentation patch for `23.2.0`_ release.
  - `23.2.2`_, on April 5, 2024: Issue `2031`_ patch. Don't validate placeholders in templates.

What's new?
^^^^^^^^^^^

- :doc:`../features/configuration`: A brand new :ref:`Merging files to memory <config-merging-tomemory>` by `@ebjornset`_ as a part of the :ref:`config-merging-files` feature.
  
  The ``AddOcelot`` method merges the ``ocelot.*.json`` files into a single ``ocelot.json`` file as the primary configuration file, which is written back to disk and then added to the ``IConfigurationBuilder`` for the well-known ``IConfiguration``. You can now call another ``AddOcelot`` method that adds the merged JSON directly from memory to the ``IConfigurationBuilder``, using ``AddJsonStream`` instead.
  
  See more details in :ref:`di-configuration-overview` of :doc:`../features/dependencyinjection`.

- :doc:`../features/servicefabric`: Published old undocumented :ref:`Placeholders in Service Name <sf-placeholders>` feature of :doc:`../features/servicefabric` `service discovery provider <https://ocelot.readthedocs.io/en/23.2.0/search.html?q=ServiceDiscoveryProvider>`_.

  This feature by `@FelixBoers`_ is available starting from version `13.0.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0>`_.

- :doc:`../features/qualityofservice`: A brand new `Polly`_ v8 pipelines :ref:`Extensibility <qos-extensibility>` feature by `@RaynaldM`_

What's updated?
^^^^^^^^^^^^^^^
 
- :doc:`../features/configuration`: New :ref:`Merging files to memory <config-merging-tomemory>` feature by `@ebjornset`_
- :doc:`../features/dependencyinjection`: Added new overloaded :ref:`di-configuration-addocelot` by `@ebjornset`_
- :doc:`../features/qualityofservice`: Support of new `Polly`_ v8 syntax and new :ref:`Extensibility <qos-extensibility>` feature by `@RaynaldM`_

Extra packages
^^^^^^^^^^^^^^

- `Ocelot.Provider.Polly <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_: Support of new `Polly`_ v8 syntax.

  *Polly* `8.0+ <https://github.com/App-vNext/Polly/releases>`_ versions introduced the concept of `resilience pipelines <https://www.pollydocs.org/pipelines/>`_.
  All `AddPolly extensions <https://github.com/ThreeMammals/Ocelot/blob/23.2.0/src/Ocelot.Provider.Polly/OcelotBuilderExtensions.cs>`_ have been automatically migrated from **v7** to **v8**. 
  Please note that older **v7** extensions are marked with the ``[Obsolete]`` attribute and renamed using the ``V7`` suffix. And the old **v7** implementation has been moved to the `v7 namespace <https://github.com/ThreeMammals/Ocelot/tree/23.2.0/src/Ocelot.Provider.Polly/v7>`_.

  See more details in :ref:`qos-polly-v7-vs-v8` section of :doc:`../features/qualityofservice` chapter.

Stabilization
^^^^^^^^^^^^^

- `683 <https://github.com/ThreeMammals/Ocelot/issues/683>`_ by PR `1927 <https://github.com/ThreeMammals/Ocelot/pull/1927>`_. Thanks to `@AlyHKafoury`_!

  New rules have been added to Ocelot's configuration validation logic to find duplicate placeholders in path templates.
  See more in the `FileConfigurationFluentValidator <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileConfigurationFluentValidator&type=code>`_ class.

- `1518 <https://github.com/ThreeMammals/Ocelot/issues/1518>`_ hotfix by PR `1986 <https://github.com/ThreeMammals/Ocelot/pull/1986>`_. Thanks to `@ArwynFr`_!

  Using the default ``IServiceCollection`` `DI extensions <https://github.com/ThreeMammals/Ocelot/blob/23.2.0/src/Ocelot/DependencyInjection/ServiceCollectionExtensions.cs>`_ to register Ocelot services resulted in the ``ServiceCollection`` provider being forced to be created by calling ``BuildServiceProvider()``.
  This resulted in problems with dependency injection libraries, or worse, causing the Ocelot app to crash!
  See more in the `ServiceCollectionExtensions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+ServiceCollectionExtensions&type=code>`_ class.

- See `all bugs <https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue%20state%3Aclosed%20label%3A%22Feb%2724%22%20label%3Abug>`_ of the `February'24 <https://github.com/ThreeMammals/Ocelot/milestone/5>`_ milestone

Documentation
^^^^^^^^^^^^^

- :doc:`../features/configuration`
- :doc:`../features/dependencyinjection`
- :doc:`../features/qualityofservice`
- :doc:`../features/servicefabric`

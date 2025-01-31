.. _23.4.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.2
.. _23.4.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.3
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _.NET 9: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
.. _Ocelot: https://www.nuget.org/packages/Ocelot

.. role::  htm(raw)
    :format: html

.. _welcome:

#######
Welcome
#######

Welcome to the Ocelot `24.0`_ documentation!

It is recommended to read all :ref:`release-notes` if you have deployed the Ocelot app in a production environment and are planning to upgrade to major or patched versions.

.. The major version `23.4.0`_ includes several patches, the history of which is provided below.

.. .. admonition:: Patches

..   - `23.4.1`_, on Nov 22, 2024: Routing patch.
..   - `23.4.2`_, on Nov 27, 2024: End of .NET 6/7 Support patch.

.. _release-notes:

Release Notes
-------------

| Release Tag: `24.0.0`_
| Release Codename: `.NET 9`_

  .. :htm:`<details><summary>With release jokes:</summary>`

  .. - **for men**: Wearing a cap with the `MAGA slogan <https://www.bing.com/search?q=make+america+great+again+slogan>`_ is encouraged when visiting McDonald's.
  .. - **for women**: Donald is fond of caps, particularly the `MAGA cap <https://www.bing.com/search?q=make+america+great+again+cap>`_, and it's amusing to see children's reactions when `We Ask Kids How Mr.D is Doing <https://www.youtube.com/watch?v=XYviM5xevC8>`_?
  .. - **for black men**: Here are some highlights of Donald's antics aka Mr. D:

  ..   | 1 `Mr. D stops to retrieve Marine's hat <https://www.youtube.com/watch?v=pAbgc41pksE>`_
  ..   | 2 `M-A-G-A caps take flight <https://www.youtube.com/watch?v=jJDXj6-54wE>`_
  ..   | 3 `Mr. D Dances To 'YMCA' <https://www.youtube.com/watch?v=Zph7YXfjMhg>`_
  ..   | 4 `Elon is more than just a MAGAr <https://www.youtube.com/watch?v=zWSXmMiWTJ0&t=42s>`_
  ..   | 5 `Mr. D looks for a job at McDonald's in 2024 <https://www.youtube.com/watch?v=_PgYAPdOs9M>`_
  ..   | lastly, `Mr. D serves customers at McDonald's Drive-Thru <https://www.youtube.com/watch?v=RwWDCh8O9WE>`_

  .. :htm:`</details>`

About
^^^^^

On November 12th, 2024, the `.NET team <https://devblogs.microsoft.com/dotnet/author/dotnet/>`_ announced the release of the `.NET 9`_ framework:

* `Announcing .NET 9 | .NET Blog <https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/>`_

This major release upgrades `Ocelot`_ package `TFMs <https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions>`_ to ``net9.0`` in addition to the current ``net8.0``.
Thus, the current Ocelot `supported frameworks <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle>`_ are .NET 8 LTS and .NET 9 STS.
According to the `.NET Support Policy <https://dotnet.microsoft.com/en-us/platform/support/policy>`_, the Ocelot team has discontinued support of .NET 6 and .NET 7 by providing the version `23.4.3`_ which targets those .NET versions.

.. What's New?
.. ^^^^^^^^^^^

.. - :doc:`../features/routing`: Introducing the new ":ref:`routing-embedded-placeholders`" feature by `@ggnaegi`_.

..   | As of November 2024, Ocelot was unable to process multiple :ref:`routing-placeholders` embedded between two forward slashes (``/``). It was also challenging to differentiate the placeholder from other elements within the slashes. For example, ``/{url}-2/`` for ``/y-2/`` would yield ``{url} = y-2``. We are excited to introduce an enhanced method for evaluating placeholders that allows for the resolution of :ref:`routing-placeholders` within complex URLs.
..   | For additional information, refer to PR `2200`_.

What's Updated?
^^^^^^^^^^^^^^^

  ``TODO``: To be written...

.. .. _1912: https://github.com/ThreeMammals/Ocelot/issues/1912
.. .. _2218: https://github.com/ThreeMammals/Ocelot/issues/2218

.. - `Core <https://github.com/ThreeMammals/Ocelot/labels/Core>`_: The main `Ocelot`_ package and all `extension <https://www.nuget.org/profiles/ThreeMammals>`_ packages `reference <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%3CTargetFrameworks%3E&type=code>`_ ``net8.0`` and ``net9.0`` target framework monikers.
..   The ``net6.0`` and ``net7.0`` TFMs were removed.

..   Curious? Search for all references: `<TargetFrameworks> <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%3CTargetFrameworks%3E&type=code>`_.

.. - :doc:`../features/authentication`: Stopped testing :ref:`authentication-identity-server` functionality.

..   The reason is explained in the `23.4.2`_ release notes warnings.

.. - :doc:`../features/administration`: The ``Ocelot.Administration`` extension package was renamed to ``Ocelot.Administration.IdentityServer4`` with immediate deprecation.

..   To address all `IdentityServer4 <https://github.com/IdentityServer/IdentityServer4>`_-related `vulnerabilities <https://github.com/ThreeMammals/Ocelot/security/dependabot>`_ (issue `2218`_), the package source code has been `moved out of the Ocelot repository <https://github.com/ThreeMammals/Ocelot/tree/23.4.2/src/Ocelot.Administration>`_.
..   The feature was solely based on the `IdentityServer4 package <https://github.com/ThreeMammals/Ocelot/blob/23.4.2/src/Ocelot.Administration/Ocelot.Administration.csproj#L38>`_, whose `repository <https://github.com/IdentityServer/IdentityServer4>`_ was archived by the owner on July 31, 2024.
..   The Ocelot team deprecated the new ``Ocelot.Administration.IdentityServer4`` extension package, but it will not be archived; any patches will be possible in the future.

.. - :doc:`../introduction/gettingstarted`: Learning :ref:`gettingstarted-samples` projects were reviewed, rewritten, and refactored due to issue `1912`_.

Documentation Summary
^^^^^^^^^^^^^^^^^^^^^

  ``TODO``: To be written...

.. - :doc:`../introduction/gettingstarted`: Completely rewritten due to the fixed issue `1912`_.
..   Now documentation C# code blocks provide code snippets using `top-level statements <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements>`_ syntax.

Contributing
------------

.. |octocat| image:: images/octocat.png
  :alt: octocat
  :height: 30
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

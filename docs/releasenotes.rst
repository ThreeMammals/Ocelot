.. _2299: https://github.com/ThreeMammals/Ocelot/issues/2299
.. _23.4.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.2
.. _23.4.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.3
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.1
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _24.1.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _.NET 9: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
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

..   - `24.0.1`_, on July 16, 2025: Issue `2299`_ patch for the `Ocelot.Provider.Kubernetes`_ extension package.

.. _release-notes:

Release Notes
-------------

  | Release Tag: `24.1.0`_
  | Release Codename: `Globality`_

.. On November 12th, 2024, the `.NET team <https://devblogs.microsoft.com/dotnet/author/dotnet/>`_ announced the release of the `.NET 9`_ framework:

.. * `Announcing .NET 9 | .NET Blog <https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/>`_

.. This major release upgrades `Ocelot`_ package `TFMs <https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions>`_ to ``net9.0`` in addition to the current ``net8.0``.
.. Thus, the current Ocelot `supported frameworks <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle>`_ are .NET 8 LTS and .NET 9 STS.
.. According to the `.NET Support Policy <https://dotnet.microsoft.com/en-us/platform/support/policy>`_, the Ocelot team has discontinued support of .NET 6 and .NET 7 by providing the version `23.4.3`_ which targets those .NET versions.

Global configuration for all features.

What's New?
-----------

.. _@raman-m: https://github.com/raman-m
.. _DevOps: https://github.com/ThreeMammals/Ocelot/labels/DevOps

- `DevOps`_: The CI/CD infrastructure was migrated from CircleCI to GitHub Actions by `@raman-m`_.

  .. _PR: https://github.com/ThreeMammals/Ocelot/blob/main/.github/workflows/pr.yml
  .. _Develop: https://github.com/ThreeMammals/Ocelot/blob/main/.github/workflows/develop.yml
  .. _Release: https://github.com/ThreeMammals/Ocelot/blob/main/.github/workflows/release.yml
  .. _three workflows: https://github.com/ThreeMammals/Ocelot/tree/main/.github/workflows
  .. _documentation: https://docs.github.com/en/actions
  .. _GitHub Actions: https://github.com/features/actions
  .. _Coveralls: https://coveralls.io/
  .. _ThreeMammals/Ocelot: https://coveralls.io/github/ThreeMammals/Ocelot

  Starting from version `24.0`_, all pull requests, development commits, and releases will be built using `GitHub Actions`_ workflows (`documentation`_).
  We currently have `three workflows`_: one for pull requests (`PR`_), one for the ``develop`` branch (`Develop`_), and one for the ``main`` branch (`Release`_).
  All workflow runs are available on the `Actions dashboard <https://github.com/ThreeMammals/Ocelot/actions>`_.

What's Updated?
---------------

.. _1912: https://github.com/ThreeMammals/Ocelot/issues/1912
.. _2218: https://github.com/ThreeMammals/Ocelot/issues/2218
.. _2274: https://github.com/ThreeMammals/Ocelot/pull/2274
.. _TargetFrameworks: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%3CTargetFrameworks%3E&type=code
.. _reference: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%3CTargetFrameworks%3E&type=code
.. _extension: https://www.nuget.org/profiles/ThreeMammals
.. _vulnerabilities: https://github.com/ThreeMammals/Ocelot/security/dependabot
.. _ASP.NET Core Identity: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
.. _acceptance testing: https://github.com/ThreeMammals/Ocelot/tree/develop/test/Ocelot.AcceptanceTests
.. _Microsoft.AspNetCore.Authentication.JwtBearer: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer
.. _IdentityServer4: https://github.com/DuendeArchive/IdentityServer4

.. - |Core|_:

- `Core <https://github.com/ThreeMammals/Ocelot/labels/Core>`_:

  The main `Ocelot`_ package and all `extension`_ packages `reference`_ ``net8.0`` and ``net9.0`` target framework monikers (TFMs).
  Refer to `TargetFrameworks`_ to verify this.
  The ``net6.0`` and ``net7.0`` TFMs have been removed.
  If your project still relies on these outdated TFMs, please continue using version `23.4.3`_.

  .. |Core| replace:: **Core**
  .. _Core: https://github.com/ThreeMammals/Ocelot/labels/Core

- :doc:`../features/authentication`:

  Testing of :ref:`authentication-identity-server` functionality was stopped due to `vulnerabilities`_ reported by Dependabot,
  specifically the "`IdentityServer Open Redirect vulnerability <https://github.com/ThreeMammals/Ocelot/security/dependabot?q=is%3Aclosed+IdentityServer>`_" security issue.
  More technical details were provided in the `23.4.2`_ release notes, where we notified the community.
  Ultimately, issue `2218`_ was addressed via pull request `2274`_.

    **Note**: In upcoming releases, we plan to utilize the `ASP.NET Core Identity`_ framework in our `acceptance testing`_ project to align with .NET industry standards.
    As a result, we intend to replace the `IdentityServer4`_ library with `ASP.NET Core Identity`_, which also supports Bearer tokens, also known as ``JwtBearerHandler`` from the `Microsoft.AspNetCore.Authentication.JwtBearer`_ namespace.

- :doc:`../features/administration`:

  The `Ocelot.Administration`_ extension package has been renamed to `Ocelot.Administration.IdentityServer4`_ (it is scheduled for deprecation) to address all `IdentityServer4`_-related `vulnerabilities`_ (issue `2218`_).
  The `package's source code <https://github.com/ThreeMammals/Ocelot/tree/release/23.4/src/Ocelot.Administration>`_ has been moved out of the Ocelot repository (pull request `2274`_) and transferred to the newly created `Ocelot.Administration.IdentityServer4`_ repository.

    **Note**: Currently, the :doc:`../features/administration` feature is solely based on the `IdentityServer4 package <https://github.com/ThreeMammals/Ocelot/blob/release/23.4/src/Ocelot.Administration/Ocelot.Administration.csproj#L38>`_, whose `repository <https://github.com/IdentityServer/IdentityServer4>`_ was archived by its owner on July 31, 2024.
    The Ocelot team will deprecate the new `Ocelot.Administration.IdentityServer4`_ extension package after the current Ocelot release; however, the repository will not be archived, allowing for potential patches in the future.

  .. _Ocelot.Administration: https://www.nuget.org/packages/Ocelot.Administration
  .. _Ocelot.Administration.IdentityServer4: https://github.com/ThreeMammals/Ocelot.Administration.IdentityServer4

- :doc:`../features/kubernetes`:

  1. Answered question `2256`_ on "How to provide a host to the Kubernetes service discovery provider?"
     Unfortunately, in the :doc:`../features/kubernetes` chapter, it was unclear to users how to define a K8s endpoint host in the :ref:`k8s-configuration` due to the implicit reuse of ``KubeClient``, which is created from the pod account during :ref:`k8s-install`-ation.
     As a team, we decided to add the new :ref:`k8s-addkubernetes-action-method`, which handles different user scenarios.
     It is now possible to provide manually configured ``KubeClientOptions`` in C# during :ref:`k8s-install`-ation, but users can also reuse ``ServiceDiscoveryProvider`` options from the global :ref:`k8s-configuration`, including the ``Host`` option to construct the :doc:`../features/kubernetes` endpoint address.
     The new overloaded ``AddKubernetes(Action<KubeClientOptions>)`` method was implemented in pull request `2257`_.

  2. In the `Ocelot.Provider.Kubernetes`_ extension package, the ``KubeClient`` dependency library version was upgraded to ``3.0.x``, which requires .NET 8.0 and .NET 9.0 TFMs for the current Ocelot version `24.0`_.
     ``KubeClient`` v3 was internally reviewed and released specifically to meet Ocelot's needs for this release. Thanks to Adam Friedman (`@tintoy`_) for his collaboration!
     This package upgrade was implemented in pull request `2266`_.

  .. _2256: https://github.com/ThreeMammals/Ocelot/discussions/2256
  .. _2257: https://github.com/ThreeMammals/Ocelot/pull/2257
  .. _2266: https://github.com/ThreeMammals/Ocelot/pull/2266
  .. _Ocelot.Provider.Kubernetes: https://www.nuget.org/packages/Ocelot.Provider.Kubernetes/
  .. _@tintoy: https://github.com/tintoy

- `Sample <https://github.com/ThreeMammals/Ocelot/labels/sample>`_:

  The learning `Samples`_ projects were reviewed, rewritten, and refactored due to issue `1912`_.
  The community brought to our attention that the documentation and `Samples`_ were outdated, as .NET 8 allows the ``Program.cs`` file to be minimized using the `Top-level statements`_ feature.
  This was ultimately addressed in pull requests `2244`_ and `2258`_.

  .. _2244: https://github.com/ThreeMammals/Ocelot/pull/2244
  .. _2258: https://github.com/ThreeMammals/Ocelot/pull/2258
  .. _Samples: https://github.com/ThreeMammals/Ocelot/tree/main/samples
  .. _Top-level statements: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements

Documentation Summary
---------------------

Due to the major version increase to v24, all documentation chapters were reviewed to improve readability, eliminate ambiguity, provide more useful tables and data schemas, update code snippets with the syntax of `Top-level statements`_, and add handy samples, among other enhancements.
The entire documentation is designed to be truly professional for senior developers while remaining easy to read for junior developers and newcomers who are starting to use the Ocelot gateway.

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

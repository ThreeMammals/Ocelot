Release Process
===============

* The *release process* is optimized when using Gitflow branching, as detailed here: `Gitflow Workflow <https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow>`_.
  It's important to note that the Ocelot team does not utilize `GitHub Flow <https://docs.github.com/en/get-started/using-github/github-flow>`_, which, despite being quicker, does not align with the efficiency required for Ocelot's delivery.
* Contributors are free to manage their pull requests and feature branches as they see fit to contribute to the `develop <https://github.com/ThreeMammals/Ocelot/tree/develop>`_ branch.
* Maintainers have the autonomy to handle pull requests and merges. Any merges to the `main <https://github.com/ThreeMammals/Ocelot/tree/main>`_ branch will trigger the release of packages to GitHub and NuGet.
* In conclusion, while users should adhere to the guidelines in :doc:`../building/devprocess`, maintainers should follow the procedures outlined in :doc:`../building/releaseprocess`.

Ocelot project follows this *release process* to incorporate work into NuGet packages:

1. Maintainers review pull requests and, if satisfactory, merge them; otherwise, they provide feedback for the contributor to address.
   Contributors are supported through continuous `Pair Programming <https://www.bing.com/search?q=Pair+Programming>`_ sessions, which include multiple code reviews, resolving code review issues, and problem-solving.

2. Maintainers must adhere to Semantic Versioning (`SemVer <https://semver.org/>`_) supported by `GitVersion <https://gitversion.net/docs/>`_.
   For breaking changes, maintainers should use the correct commit message to ensure **GitVersion** applies the appropriate **SemVer** tags.
   Manual tagging of the Ocelot repository should be avoided to prevent disruptions.

3. Once a pull request is merged into the `develop`_ branch, the `Ocelot NuGet packages <https://www.nuget.org/profiles/ThreeMammals>`_ remain unchanged until a release is initiated.
   When sufficient work warrants a new release, the `develop`_ branch is merged into `main`_ as a ``release/X.Y.Z`` branch, triggering the release process that builds the code, assigns versions, and pushes artifacts to GitHub and NuGet packages to NuGet.

4. The release engineer, who holds the integration tokens for both CircleCi and GitHub, automates each release build using the primary build script, ``build.cake``.
   The release engineer is also responsible for DevOps within the organization, across all (sub)repositories, supporting the primary build script.

5. The release engineer drafts ``ReleaseNotes.md``, informing the community about key aspects of the release, including new or updated features, bug fixes, documentation updates, breaking changes, contributor acknowledgments, version upgrade guidelines, etc.

6. The final step involves returning to GitHub to close the current `milestone <https://github.com/ThreeMammals/Ocelot/milestones>`_, ensuring that:

   * All issues within the `milestone`_ are closed; any remaining work from open issues should be transferred to the next `milestone`_.
   * All pull requests associated with the `milestone`_ are either closed or reassigned to the upcoming release `milestone`_.
   * Release Notes are published on GitHub `releases <https://github.com/ThreeMammals/Ocelot/releases>`_, with an additional review of the text.
   * The published release is designated as the latest, provided the corresponding `Ocelot NuGet packages`_ have been successfully uploaded to the `ThreeMammals <https://www.nuget.org/profiles/ThreeMammals>`_ account.

7. Optional support for the major version ``X.Y.0`` should be available in instances such as Microsoft official patches and critical Ocelot defects of the major version.
   Maintainers should release patched versions ``X.Y.1-z`` as hot-fix patch versions.

Notes
-----

All NuGet package builds and releases are conducted through CircleCI.
For details, refer to the `Pipelines - ThreeMammals/Ocelot <https://circleci.com/gh/ThreeMammals/Ocelot/>`_ on CircleCI.

Currently, only `Tom Pallister <https://github.com/TomPallister>`_, `Raman Maksimchuk <https://github.com/raman-m>`_, the owners, along with the `Ocelot Team <https://github.com/orgs/ThreeMammals/teams>`_ maintainers, have the authority to merge releases into the `main`_ branch of the Ocelot repository.
This policy is to ensure that a final :ref:`quality-gates` are in place.
The maintainers' primary focus during the final merge is to identify any security issues, as outlined in Step **7** of the process.

.. _quality-gates:

Quality Gates
-------------

    To be developed...

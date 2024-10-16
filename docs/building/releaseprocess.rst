Release Process
===============

* The release process works best with `Gitflow <https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow>`_ branching.
  Note, Ocelot team doesn't use `GitHub flow <https://docs.github.com/en/get-started/using-github/github-flow>`_ which is faster but not efficient in Ocelot delivery.
* Contributors can do whatever they want on pull requests and feature branches to deliver a feature to **develop** branch.
* Maintainers can do whatever they want on pull requests and merges to **main** will result in packages being released to GitHub and NuGet.
* Finally, users follow :doc:`../building/devprocess`, but maintainers follow this :doc:`../building/releaseprocess`.

Ocelot uses the following process to accept work into the NuGet packages.

1. Maintainers provide code review of pull request and if all is good merge it, else they will suggest feedback that the user will need to act on.
   Extra help to contributors is welcomed via constant Pair Programming sessions: multiple code reviews, fixing code review issues, any problem solving.

2. The maintainer must follow the `SemVer <https://semver.org/>`_ support for this is provided by `GitVersion <https://gitversion.net/docs/>`_.
   So if the maintainer needs to make breaking changes, be sure to use the correct commit message, so **GitVersion** uses the correct **SemVer** tags.
   Do not manually tag the Ocelot repo: this will break things!

3. After the PR is merged to **develop** the Ocelot NuGet packages will not be updated until a release is created.
   And, when enough work has been completed to justify a new release, **develop** branch will be merged into **main** as ``release/X.Y.Z`` branch,
   the release process will begin which builds the code, versions it, pushes artifacts to GitHub and NuGet packages to NuGet.

4. Release engineer, the owner of integration tokens both on CircleCi and GitHub, automates each release build by the main building script aka ``build.cake``.
   Release engineer is responsible for any DevOps at the organization, in any (sub)repositories, supporting the main building script.

5. Release engineer writes `ReleaseNotes.md <https://github.com/ThreeMammals/Ocelot/blob/main/README.md>`_ notifying community about
   important artifacts of the release such as new/updated features, fixed bugs, updated documentation, breaking changes, contributors info, version upgrade instructions, etc.

6. The final step is to go back to GitHub and close current milestone ensuring the following:

   * all issues in the milestone should be closed, the rest of work of open issues should be moved to the next milestone.
   * all pull requests of the milestone should be closed, or moved to the next upcoming release milestone.
   * Release Notes should be published to GitHub releases, with extra checking the text.
   * Published release must be marked as the latest, if appropriate Nuget packages were successfully uploaded to `NuGet Gallery | ThreeMammals <https://www.nuget.org/profiles/ThreeMammals>`_ account.

7. Optional support of the major version ``2X.Y.0`` should be provided in such cases as Microsoft official patches, critical Ocelot defects of the major version.
   Maintainers release patched versions ``2X.Y.xxx`` as hot-fixing patch-versions.

Notes
-----

All NuGet package builds and releases are done with CircleCI, see `Pipelines - ThreeMammals/Ocelot <https://circleci.com/gh/ThreeMammals/Ocelot/>`_.

Only `Tom Pallister <https://github.com/TomPallister>`_, `Raman Maksimchuk <https://github.com/raman-m>`_ (owners) and maintainers from `Ocelot Team <https://github.com/orgs/ThreeMammals/teams>`_ can merge releases into `main <https://github.com/ThreeMammals/Ocelot/tree/main>`_ at the moment.
This is to ensure there is a final :ref:`quality-gates` in place.
Maintainers are mainly looking for security issues on the final merge: see Step 7 in the process.

.. _quality-gates:

Quality Gates
-------------

    To be developed...

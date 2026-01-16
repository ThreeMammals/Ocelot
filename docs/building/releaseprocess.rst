.. _Gitflow Workflow: https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow
.. _GitHub Flow: https://docs.github.com/en/get-started/using-github/github-flow
.. _develop: https://github.com/ThreeMammals/Ocelot/tree/develop
.. _main: https://github.com/ThreeMammals/Ocelot/tree/main

Release Process
===============

* The *release process* is optimized when using Gitflow branching, as detailed here: `Gitflow Workflow`_.
  It's important to note that the Ocelot team does not utilize `GitHub Flow`_, which, despite being quicker, does not align with the efficiency required for Ocelot's delivery.
* Contributors are free to manage their pull requests and feature branches as they see fit to contribute to the '`develop`_' branch.
* Maintainers have the autonomy to handle pull requests and merges. Any merges to the '`main`_' branch will trigger the release of packages to GitHub and NuGet.
* In conclusion, while users should adhere to the guidelines in :doc:`../building/devprocess`, maintainers should follow the procedures outlined in :doc:`../building/releaseprocess`.

Stages
------
.. _Pair Programming: https://www.bing.com/search?q=Pair+Programming
.. _SemVer: https://semver.org
.. _GitVersion: https://gitversion.net/docs/
.. _Ocelot NuGet packages: https://www.nuget.org/profiles/ThreeMammals
.. _Release: https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml
.. _Environments: https://github.com/ThreeMammals/Ocelot/settings/environments
.. _build.cake: https://github.com/ThreeMammals/Ocelot/blob/main/build.cake
.. _ThreeMammals: https://github.com/ThreeMammals
.. _milestone: https://github.com/ThreeMammals/Ocelot/milestones
.. _releases: https://github.com/ThreeMammals/Ocelot/releases

Ocelot project follows this *release process* to incorporate work into NuGet packages:

1. As a code reviewers, maintainers review pull requests and, if satisfactory, merge them; otherwise, they provide feedback for the contributor to address.
   Contributors are supported through continuous `Pair Programming`_ sessions, which include multiple code reviews, resolving code review issues, and problem-solving.

2. As a release engineers, maintainers must adhere to Semantic Versioning (`SemVer`_) supported by `GitVersion`_.
   For breaking changes, maintainers should use the correct commit message (containing *"+semver: breaking|major|minor|patch"*) to ensure `GitVersion`_ applies the appropriate `SemVer`_ tags.
   Manual tagging of the Ocelot repository should be avoided to prevent disruptions.

3. Once a pull request is merged into the '`develop`_' branch, the `Ocelot NuGet packages`_ remain unchanged until a release is initiated.
   When sufficient work warrants a new release, the '`develop`_' branch is merged into '`main`_' as a ``release/X.Y`` branch, triggering the `Release`_ workflow that builds the code, assigns versions, and pushes artifacts to GitHub and packages to NuGet.

4. The release engineer, who holds the integration tokens in GitHub `Environments`_, automates each release build using the primary build script, '`build.cake`_'.
   Automated or manual :doc:`../building/building` can be performed :ref:`b-in-terminal` or :ref:`b-with-ci-cd`.
   The release engineer is also responsible for DevOps within the `ThreeMammals`_ organization, across all (sub)repositories, supporting the primary build script, and scripting for other repositories.

5. The release engineer drafts the ``ReleaseNotes.md`` template file, informing the community about key aspects of the release, including new or updated features, bug fixes, documentation updates, breaking changes, contributor acknowledgments, version upgrade guidelines, and more.

6. The final stage of the *release process* involves returning to GitHub to close the current `milestone`_, ensuring that:

   * All issues within the `milestone`_ are closed; any remaining work from open issues should be transferred to the next `milestone`_.
   * All pull requests associated with the `milestone`_ are either closed or reassigned to the upcoming release `milestone`_.
   * Release Notes are published on GitHub `releases`_, with an additional review of the text.
   * The published release is designated as the latest, provided the corresponding `Ocelot NuGet packages`_ have been successfully uploaded to the `ThreeMammals <https://www.nuget.org/profiles/ThreeMammals>`__ account.

7. Optional support for the major version ``X.Y.0`` should be available in cases such as Microsoft official patches and critical Ocelot defects of that major version.
   Maintainers should release patched versions ``X.Y.1-z`` as hot-fix patch versions.

Notes
-----
.. _GitHub Actions: https://docs.github.com/en/actions
.. _Actions: https://github.com/ThreeMammals/Ocelot/actions
.. _Tom Pallister: https://github.com/TomPallister
.. _Raman Maksimchuk: https://github.com/raman-m
.. _Ocelot Team: https://github.com/orgs/ThreeMammals/teams

**Note 1**: All NuGet package builds and releases are conducted through the `GitHub Actions`_ CI/CD provider.
For details, refer to the dedicated `Actions`_ dashboard, which should be used to monitor the current status of three workflows.

**Note 2**: Currently, only `Tom Pallister`_, `Raman Maksimchuk`_, the owners—along with the `Ocelot Team`_ maintainers—have the authority to merge releases into the '`main`_' branch of the Ocelot repository.
This policy ensures that final :ref:`quality-gates` are in place.
The maintainers' primary focus during the final merge is to identify any security issues, as outlined in Stage 7 of the process.

.. _quality-gates:

Quality Gates
-------------
.. _code analysis rule set: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%3CCodeAnalysisRuleSet%3E&type=code
.. _codeanalysis.ruleset: https://github.com/ThreeMammals/Ocelot/blob/main/codeanalysis.ruleset
.. _Overview of .NET source code analysis: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview?tabs=net-9
.. _StyleCop.Analyzers: https://www.nuget.org/packages/StyleCop.Analyzers
.. _reference: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20StyleCop.Analyzers&type=code
.. _here: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options

**Gate 1**: Static code analysis.
The Ocelot repository includes the following integrated style analyzers:

* In-built IDE (.NET SDK):
  The `code analysis rule set`_ is defined in the '`codeanalysis.ruleset`_' file, with configuration instructions available `here`_.
  For comprehensive documentation, refer to the following article:

  - Microsoft Learn: `Overview of .NET source code analysis`_

* `StyleCop.Analyzers`_: The package is somewhat outdated with slow support, but Ocelot projects still `reference`_ it because it has remained functional since 2015/16 as an older style analyzer.
  The Ocelot team plans to replace this library with a more advanced tool in upcoming releases.

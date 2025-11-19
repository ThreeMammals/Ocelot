.. _Gitflow Workflow: https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow
.. _GitHub Flow: https://docs.github.com/en/get-started/using-github/github-flow
.. _develop: https://github.com/ThreeMammals/Ocelot/tree/develop
.. _main: https://github.com/ThreeMammals/Ocelot/tree/main
.. _issue(s): https://github.com/ThreeMammals/Ocelot/issues
.. _discussion: https://github.com/ThreeMammals/Ocelot/discussions
.. _fork: https://docs.github.com/en/get-started/quickstart/fork-a-repo
.. _unit: https://github.com/ThreeMammals/Ocelot/tree/develop/test/Ocelot.UnitTests
.. _acceptance: https://github.com/ThreeMammals/Ocelot/tree/develop/test/Ocelot.AcceptanceTests
.. _documentation: https://github.com/ThreeMammals/Ocelot/tree/develop/docs
.. _feature: https://github.com/ThreeMammals/Ocelot/tree/develop/docs/features
.. _Actions: https://github.com/ThreeMammals/Ocelot/actions
.. _Coveralls check: https://coveralls.io/github/ThreeMammals/Ocelot

Development Process
===================

* The *development process* is optimized when using Gitflow branching, as detailed here: `Gitflow Workflow`_.
  It's important to note that the Ocelot team does not utilize `GitHub Flow`_, which, despite being quicker, does not align with the efficiency required for Ocelot's delivery.
* Contributors are free to manage their pull requests and feature branches as they see fit to contribute to the '`develop`_' branch.
* Maintainers have the autonomy to handle pull requests and merges. Any merges to the '`main`_' branch will trigger the release of packages to GitHub and NuGet.
* In conclusion, while users should adhere to the guidelines in :doc:`../building/devprocess`, maintainers should follow the procedures outlined in :doc:`../building/releaseprocess`.

Stages
------

Ocelot project follows this *development process* to integrate work into a merged commit in the '`develop`_' branch:

1. Users either create a new issue or select an existing `issue(s)`_ on GitHub.
   Issues can also be generated from `discussion`_ topics when necessary and agreed upon.

2. Users should create a `fork`_ and branch off of it (unless they are a core team member, in which case they can branch directly from the main/head/upstream repository), e.g., ``feature/xxx``, ``bug/xxx``, etc.
   The "xxx" can be the issue number or a brief description.

3. Once contributors are satisfied with their work, they can submit a pull request against the `develop`_ branch on GitHub with their changes.

4. The Ocelot team will review the pull request and, if satisfactory, merge it; otherwise, they will provide feedback for the contributor to address.
   To expedite pull request approval, contributors should consider:

   - Ensuring all changes are covered by `unit`_ and `acceptance`_ tests.
   - Ensuring that the code coverage percentage from `unit`_ tests does not decrease; thus, the `Coveralls check`_ reports a green status.
   - Updating any `documentation`_ affected by the changes, with a required review of the appropriate `feature`_ document.
   - Verifying that the feature is necessary and does not duplicate existing Ocelot features.

5. A pull request must meet the following criteria before merging:

   - All new code must be covered by `unit`_ tests.
   - There must be at least one `acceptance`_ test for the happy path of the new code.
   - Tests must pass locally, in Visual Studio Test Explorer or in terminal after performing ``dotnet test`` command.
   - The build must have a green status on repository `Actions`_ as passed *checks* of the pull request (aka `Checks`_ tab).
   - The build's performance must not be significantly degraded on repository `Actions`_ page for `PR`_ workflow.
   - The main `Ocelot package`_ must not introduce any non-Microsoft `dependencies`_.

.. _PR: https://github.com/ThreeMammals/Ocelot/actions/workflows/pr.yml
.. _Checks: https://github.com/ThreeMammals/Ocelot/pull/2283/checks
.. _Ocelot package: https://www.nuget.org/packages/Ocelot
.. _dependencies: https://www.nuget.org/packages/Ocelot#dependencies-body-tab
.. _NuGet packages: https://www.nuget.org/profiles/ThreeMammals

6. Once the pull request is merged with *"Squash and Merge"* option into the `develop`_ branch, the ``Ocelot.*`` `NuGet packages`_ will not be updated until a release is crafted.
   The concluding step involves returning to GitHub to close any resolved `issue(s)`_.

Notes
-----

**Note 1**: The `issue(s)`_ linked to the pull request within the *Development* settings (on the right sidebar of the pull request settings) will automatically close upon merging.
It is crucial for developers to utilize the *"Link an issue from this repository"* feature in the *Development* settings.
An alternative way to link `issue(s)`_ is by specifying them in the pull request description, where the developer lists the linked `issue(s)`_ that need to be closed.
For example:

.. code-block:: markdown

  ## Fixes #1222
  - #1222

  ## Closes #1333
  - #1333

  ## Proposed Changes
  - change 1
  - change 2  

This Markdown should automatically link the desired bug/issue in the open status.
For bugs, the developer needs to write "Fixes #xxxx", and for features, "Closes #xxxx".

.. _GitHub Actions: https://docs.github.com/en/actions
.. _workflows: https://github.com/ThreeMammals/Ocelot/tree/main/.github/workflows 

**Note 2**: All pull request builds are conducted using `GitHub Actions`_, but developers have the freedom to build Ocelot as needed.
Details can be found in the :doc:`../building/building` chapter.
Additionally, for a deeper understanding of the current Ocelot CI/CD environment and a clearer view of the CI/CD build process, refer to the "Building :ref:`b-with-ci-cd`" section.

**Note 3**: Should you encounter any confusion or obstacles, do not hesitate to reach out to the members of the 'Ocelot Team' or the repository maintainers.

.. _dev-best-practices:

Best Practices
--------------
* Refer to the Ocelot `Actions`_ dashboard on GitHub to verify the latest build statuses for the three current `workflows`_.
  It is recommended to monitor the build status of each workflow on the `Actions`_ dashboard or directly in the `Checks`_ tab of a pull request.
  If a build fails, initiate a new build by pushing a new commit, or consult with online maintainers or code reviewers to ensure the current pull request build is successful.
* Request a code review after reaching the "Development Complete" stage, and address all feedback issues.
  Code is deemed complete when robust code, relevant `unit`_ and `acceptance`_ tests, and `documentation`_ updates are in place.
* Set up your development environment on Windows OS using Visual Studio IDE.
  While development in Linux OS with alternative IDEs is possible, it is not recommended. For more details, refer to the :ref:`dev-fun` section.
* Remain online after submitting a pull request/issue to ensure maintainers can reach you promptly.
  Note that if you are offline for extended periods, such as days, weeks, or months, maintainers may deprioritize your work.
  A strong contribution ethic implies constant online presence and proactivity.

.. _dev-fun:

Dev Fun
-------

This section is part of the :ref:`dev-best-practices` and is written to be more amusing D)

EOL Gotchas
^^^^^^^^^^^

  *Also known as, "Line-Endings problem"*

  Since the project's inception in 2016, this issue has been persistent.
  Indeed, some lines end with the ``LF`` character, typical of the Linux OS.
  Many of our contributors work on Linux and use IDEs like Visual Studio Code, JetBrains .NET Rider, which defaults to the ``LF`` as the newline character.
  As a result, we have numerous files with inconsistent or mixed EOL characters.

This problem stems from the well-known dilemma of End-of-Line (EOL) characters in cross-OS development.
For the Windows OS, the EOL character is ``CRLF``, while for Linux, it is ``LF``.
Modern IDEs and Git repositories have their own strategies for detecting inconsistencies of mixed EOLs in source files.
However, the GitHub "Files Changed" tool unfortunately registers a line change in two scenarios: ``CRLF`` to ``LF`` and ``LF`` to ``CRLF``, even when there's no actual code change!
Reviewing such pull requests with fictitious ("fake") changes is always challenging because the reviewer's focus should be on actual code changes.

  Please note, if a pull request is filled with "fake" changes in *"Files Changed"*, the code reviewer has the right to not provide a code review, mark the PR as a draft, or even close it.

Our standard practice is to maintain end-of-line characters as they are.
Moreover, we utilize Visual Studio's unique ``.editorconfig`` IDE analyzer settings for EOL to avoid issues with line endings.
These settings are specific to Visual Studio, hence we recommend rebasing a feature branch onto develop using Visual Studio exclusively.

    Special EOL settings can be specified in the ``.gitattributes`` file of the git repository, although we do not currently manage this.

Our current recommendations for addressing the end-of-line (EOL) issue are as follows:

* Ideally, resolve merge conflicts by prioritizing the changes in the `develop`_ branch, then manually incorporate your changes in the merge tool dialog.
  It appears that changes from the feature branch are being included, even if they are minor.
  Conflicts should be addressed by manually applying your changes to the `develop`_ branch with a merge tool.

* If changes from the feature branch are given priority (despite being minor), the merge tool will document them and apply ``CRLF`` end-of-line characters according to the rules specified in ``.editorconfig``.
  This is the source of the issue.

* Renaming a method in an IDE, such as Visual Studio, or using another auto-refactoring command, causes Visual Studio to apply the command using the default styling rules in ``.editorconfig``, which includes `CRLF settings <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20end_of_line&type=code>`_.
  Thus, applying auto-refactoring commands inadvertently alters the EOL characters, leading to "fake" changes in pull requests.
  Note that Visual Studio analyzers (IDE, StyleCop, etc.) may also recommend auto-refactoring, which could be applied implicitly.
  To preserve the original EOL characters, manual code editing is necessary.
  Therefore, "fake" changes result from auto-refactoring commands in IDEs like Visual Studio, Visual Code, Rider, etc.

* **Our final recommendation** is to boot into Windows, use Visual Studio Community (which is free), refrain from using auto-refactoring commands, and ensure that EOLs remain unchanged.
  If your OS differs, you **must** ensure that the appropriate settings are provided in the ``.gitattributes`` file to always commit files with ``CRLF`` EOL characters.

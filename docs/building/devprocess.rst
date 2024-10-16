Development Process
===================

* The development process works best with `Gitflow <https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow>`_ branching.
  Note, Ocelot team doesn't use `GitHub flow <https://docs.github.com/en/get-started/using-github/github-flow>`_ which is faster but not efficient in Ocelot delivery.
* Contributors can do whatever they want on pull requests and feature branches to deliver a feature to **develop** branch.
* Maintainers can do whatever they want on pull requests and merges to **main** will result in packages being released to GitHub and NuGet.
* Finally, users follow :doc:`../building/devprocess`, but maintainers follow this :doc:`../building/releaseprocess`.

Ocelot uses the following process to accept work into a merged commit in develop.


1. User creates an issue or picks up an `existing issue <https://github.com/ThreeMammals/Ocelot/issues>`_ in GitHub.
   An issue can be created by converting `discussion <https://github.com/ThreeMammals/Ocelot/discussions>`_ topics if necessary and agreed upon.

2. User creates `a fork <https://docs.github.com/en/get-started/quickstart/fork-a-repo>`_ and branches from this
   (unless a member of core team, they can just create a branch on the head repo) e.g. ``feature/xxx``, ``bug/xxx`` etc.
   It doesn't really matter what the "xxx" is. It might make sense to use the issue number and maybe a short description. 

3. When the contributor is happy with their work they can create a pull request against **develop** in GitHub with their changes.

4. The Ocelot team will provide code review the PR and if all is good merge it, else they will suggest feedback that the user will need to act on.
   In order to speed up getting a PR the contributor should think about the following:

   - Have I covered all my changes with tests at unit and acceptance level?
   - Have I updated any documentation that my changes may have affected?
   - Does my feature make sense, have I checked all of Ocelot's other features to make sure it doesn't already exist?

   In order for a PR to be merged the following must have occured:

   - All new code is covered by unit tests.
   - All new code has at least 1 acceptance test covering the happy path.
   - Tests must have passed locally.
   - Build must have green status.
   - Build must not have slowed down dramatically.
   - The main Ocelot package must not have taken on any non MS dependencies.

6. After the PR is merged to **develop** the Ocelot NuGet packages will not be updated until a release is created!
   The final step is to go back to GitHub and close any issues that are now fixed.
   **Note**: All linked issues to the PR in **Development** settings (right side PR settings) will be closed automatically while merging the PR.
   It is imperative that developer uses the "**Link an issue from this repository**" pop-up dialog of the **Development** settings!

Notes
-----

All PR builds are done with CircleCI, see `Pipelines - ThreeMammals/Ocelot <https://circleci.com/gh/ThreeMammals/Ocelot/>`_.
It is advisable to watch for build status, and if it is failed, trigger new build or ask online maintainers or code reviewers to make sure the current PR build is green.

If anything is unclear or you get stuck in the process, please contact the `Ocelot Core Team <https://github.com/orgs/ThreeMammals/teams/ocelot-core>`_ members or repository maintainers.

.. _dev-best-practices:

Best Practices
--------------

* Ask for code review after Dev Complete stage, and resolve all issues in a provided feedback. Code is complete when solid code, appropriate unit and acceptance tests and docs update are written. 
* Organize your development environment in Windows OS utilizing Visual Studio IDE. You can develop in Linux with other IDEs, but we don't recommend that. See more details in :ref:`dev-fun` subsection.
* Ensure you are always online after creation of the PR/issue, so maintainers will contact you as fastest as they can.
  Note, if you will be offline for a days, weeks, months, then maintainers have a right to put your work in low priority.
  Your intention to contribute should be high which means to be always online and proactive.

.. _dev-fun:

Dev Fun
--------

This is a part of :ref:`dev-best-practices` but it is more funny D)

Line-ending gotchas aka EOL fun
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    This issue has persisted since the project's inception in 2016!
    Indeed, some lines end with the LF-character from the Linux OS.
    Several of our contributors work on Linux and use IDEs like Visual Code, where the default newline character is LF.
    Consequently, we have numerous files with inconsistent/mixed EOL characters.

This problem is related to well-known End-of-Line characters dillema in cross-OS development.
For Windows OS the EOL char is ``CRLF`` but for Linux it is ``LF``.
Modern IDEs and Git repos detect inconsistancy of mixed EOLs in source files following own strategies.
But GitHub "Files Changed" tool unfortunately detects a line change in these 2 scenarios: ``CRLF`` to ``LF`` and ``LF`` to ``CRLF`` changes, even there was no actual code change!
Such a pull requests with fictitious ("fake") changes are always hard to review because the focus of the reviwer should be paid to actual code changes.

    Please note, if the pull request is full of "fake" changes in **Files Changed** then code reviewer has a right not providing a code review marking PR as draft, or even closing it!

It's our common practice not to alter end-of-line characters.
Additionally, we employ Visual Studio's specific `.editorconfig <https://github.com/ThreeMammals/Ocelot/blob/develop/.editorconfig>`_ IDE analyzer settings for EOL to circumvent these line ending issues.
These settings are exclusive to Visual Studio, which is why we advise rebasing a feature branch onto develop solely using Visual Studio.

    Special EOL settings can be provided in ``.gitattributes`` file of the git repository. But we don't handle this currently.

Our current recommendations for addressing the EOL issue are:

* It's preferable to resolve merge conflicts while honoring the changes in the develop branch.
  It appears that changes are being collected from the feature branch, even when there are no substantial changes.
  However, conflicts should be resolved by applying your changes onto the develop branch using a merging tool.

* If changes from the feature branch are prioritized (despite being insignificant), the merge tool will record them and apply CRLF end-of-line characters based on the rules specified in ``.editorconfig``.
  This is where the issue arises.

* When you rename a method in IDE, for instance in Visual Studio, or use another auto-refactoring command, Visual Studio applies the command using the default styling rules in ``.editorconfig``,
  which includes `CRLF settings <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20end_of_line&type=code>`_.
  Therefore, applying auto-refactoring commands implicitly changes the EOL characters! This is the source of "fake" changes in PRs.
  Please note, Visual Studio analyzers (IDE, StyleCop, etc.) recommends auto-refactoring too which could be applied implicitly.
  To maintain the original EOL characters, you must edit the code manually!
  So, fictitious ("fake") changes are the result of auto-refactoring commands in IDEs such as Visual Studio, Visual Code, Rider, and others.

* **Our final recommendations: Boot into Windows, use Visual Studio Community (it's free), avoid using auto-refactoring commands, and EOLs should remain unchanged.**

Release Process
===============

* The release process works best with `Gitflow <https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow>`_ branching. 
* Contributors can do whatever they want on PRs and feature branches to deliver a feature to **develop** branch.
* Maintainers can do whatever they want on PRs and merges to **main** will result in packages being released to GitHub and NuGet.

Ocelot uses the following process to accept work into the NuGet packages.

1. User creates an issue or picks up an `existing issue <https://github.com/ThreeMammals/Ocelot/issues>`_ in GitHub.
   An issue can be created by converting `discussion <https://github.com/ThreeMammals/Ocelot/discussions>`_ topics if necessary and agreed upon.

2. User creates `a fork <https://docs.github.com/en/get-started/quickstart/fork-a-repo>`_ and branches from this (unless a member of core team, they can just create a branch on the head repo) e.g. ``feature/xxx``, ``bug/xxx`` etc.
   It doesn't really matter what the "xxx" is. It might make sense to use the issue number and maybe a short description. 

3. When the contributor is happy with their work they can create a pull request against **develop** in GitHub with their changes.

4. The maintainer must follow the `SemVer <https://semver.org/>`_ support for this is provided by `GitVersion <https://gitversion.net/docs/>`_.
   So if the maintainer needs to make breaking changes, be sure to use the correct commit message, so **GitVersion** uses the correct **SemVer** tags.
   Do not manually tag the Ocelot repo: this will break things!

5. The Ocelot team will review the PR and if all is good merge it, else they will suggest feedback that the user will need to act on.

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

6. After the PR is merged to **develop** the Ocelot NuGet packages will not be updated until a release is created.

7. When enough work has been completed to justify a new release,
   **develop** branch will be merged into **main** as **release/xxx** branch, the release process will begin which builds the code, versions it, pushes artifacts to GitHub and NuGet packages to NuGet.

8. The final step is to go back to GitHub and close any issues that are now fixed.
   **Note**: All linked issues to the PR in **Development** settings (right side PR settings) will be closed automatically while merging the PR.
   It is imperative that developer uses the "**Link an issue from this repository**" pop-up dialog of the **Development** settings!

Notes
-----

All NuGet package builds and releases are done with CircleCI, see `Pipelines - ThreeMammals/Ocelot <https://circleci.com/gh/ThreeMammals/Ocelot/>`_.

Only Tom Pallister (owner) and Ocelot Core Team members (maintainers) can merge releases into **main** at the moment.
This is to ensure there is a final `quality gate <#quality-gates>`_ in place. Tom is mainly looking for security issues on the final merge.

We **do** follow this development and release process!
If anything is unclear or you get stuck in the process, please contact the `Ocelot Core Team <https://github.com/orgs/ThreeMammals/teams/ocelot-core>`_ members or repository maintainers.

Quality Gates
-------------

    To be developed...

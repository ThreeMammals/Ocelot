Release process
===============

Ocelot uses the following process to accept work into the NuGet packages.

1. User creates an issue or picks up an existing issue in GitHub. 

2. User creates a fork and branches from this (unless a member of core team, they can just create a branch on the main repo) e.g. feat/xxx, fix/xxx etc. It doesn't really matter what the xxx is. It might make sense to use the issue number and maybe a short description. I don't care as long as it has (feat, fix, refactor)/xxx :) 

3. When the user is happy with their work they can create a pull request in GitHub with their changes. The user must follow the `SemVer <https://semver.org/>`_ support for this is provided by `GitVersion <https://gitversion.readthedocs.io/en/latest/>`_. So if you need to make breaking changes please make sure you use the correct commit message so GitVersion uses the correct semver tags. Do not manually tag the Ocelot repo this will break things.

4. The Ocelot team will review the PR and if all is good merge it, else they will suggest feedback that the user will need to act on. In order to speed up getting a PR the user should think about the following.
    - Have I covered all my changes with tests at unit and acceptance level?
    - Have I updated any documentation that my changes may have affected?
    - Does my feature make sense, have I checked all of Ocelot's other features to make sure it doesn't already exist?
In order for a PR to be merged the following must have occured.
    - All new code is covered by unit tests.
    - All new code has at least 1 acceptance test covering the happy path.
    - Builds for Windows, Mac and Linux must have passed.
    - Builds for Windows, Mac and Linux must not have slowed down dramatically.
    - The main Ocelot package must not have taken on any non MS dependencies.

5. After the PR is merged the GitHub issue must be labelled as merged. The merge will trigger an alpha build on the develop branch with these changes that will get pushed to NuGet. You can import this and test it manually should you wish.

6. When the Ocelot team is ready to create a release they will checkout a new release branch with the version from the latest develop build. So look in AppVeyor for the latest develop semver number and checkout a new branch e.g. git checkout -b release/13.1.0 and push this to the remote. Wait for it to build and then merge this branch back into master.

7. Wait for the master build to complete. When it has go to the AppVeyor UI and find the build and click the Deploy link in the top right hand corner. Select release preparation from the environment drop down and click deploy. This will send the release information to GitHub releases. 

8. Go to GitHub releases and find the version you have just released. It will look something like 13.0.0+31.build.1783. Remove +31.build.1783 (or whatever you get) from all the input fields so you are just left with 13.0.0. Untick This is a pre release and click release. This will trigger a build from AppVeyor that downloads the release artifacts from GitHub and publishes them to the stable NuGet feed. All being well you should find your new package on NuGet within 30 minutes. You might also want to manually add the issue numbers of what has been merged so people can see what changed in this release.

9. The final step is to go back to GitHub and close any issues that were labelled as merged. You should see something like this in`GitHub <https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0>`_ and this in `NuGet <https://www.nuget.org/packages/Ocelot/13.0.0>`_.

Notes
-----

All NuGet package builds are done with AppVeyor `here <https://ci.appveyor.com/project/TomPallister/ocelot-fcfpb>_` and all releases are done from `here <https://ci.appveyor.com/project/TomPallister/ocelot-ayj4w>_`. We also build Ocelot on Travis `here <https://travis-ci.org/ThreeMammals/Ocelot>`_ but this is only to make sure it is building on multiple OS.

Only Ocelot core team members can merge PRs and create release branches. Only TomPallister can merge releases into master at the moment. This is to ensure there is a final quality gate in place. Tom is mainly looking for security issues on the final merge.

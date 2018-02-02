Release process
===============

This section defines the release process for the maintainers of the project.
* Merge pull requests to the `release` branch.

* Every commit pushed to the Origin repo will kick off the `ocelot-build <https://ci.appveyor.com/project/TomPallister/ocelot-fcfpb>`_ project in AppVeyor. This performs the same tasks as the command line build, and in addition pushes the packages to the unstable nuget feed.

* When you're ready for a release, create a release branch. You'll probably want to update the committed `./ReleaseNotes.md` based on the contents of the equivalent file in the `./artifacts` directory.

* When the `release` branch has built successfully in Appveyor, select the build and then Deploy to the `GitHub Release` environment. This will create a new release in GitHub.

* In Github, navigate to the `release <https://github.com/TomPallister/Ocelot/releases>`_. Modify the release name and tag as desired.

* When you're ready, publish the release. This will tag the commit with the specified release number.

* The `ocelot-release <https://ci.appveyor.com/project/TomPallister/ocelot-ayj4w>`_ project will detect the newly created tag and kick off the release process. This will download the artifacts from GitHub, and publish the packages to the stable nuget feed.

* When you have a final stable release build, merge the `release` branch into `master` and `develop`. Deploy the master branch to github and following the full release process as described above. Don't forget to uncheck the "This is a pre-release" checkbox in GitHub before publishing.

* Note - because the release builds are initiated by tagging a commit, if for some reason a release build fails in AppVeyor you'll need to delete the tag from the repo and republish the release in GitHub.



Building
========

* You'll generally want to run the `./build.ps1` script. This will compile, run unit and acceptance tests and build the output packages locally. Output will got to the `./artifacts` directory.

* You can view the current commit's `SemVer <http://semver.org/>`_ build information by running `./version.ps1`.

* The other `./*.ps1` scripts perform subsets of the build process, if you don't want to run the full build.

* The release process works best with GitFlow branching; this allows us to publish every development commit to an unstable feed with a unique SemVer version, and then choose when to release to a stable feed.

* Alternatively you can build the project in VS2017 with the latest .NET Core SDK.
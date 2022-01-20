Building
========

* You can also just run `dotnet tool restore && dotnet cake` locally!. Output will got to the `./artifacts` directory.

* The best way to replicate the CI process is to build Ocelot locally is using the Dockerfile.build file which can be found in the docker folder in Ocelot root. Use the following command `docker build --platform linux/amd64 -f ./docker/Dockerfile.build .` for example. You will need to change the platform flag depending on your platform.

* There is a Makefile to make it easier to call the various targers in `build.cake`. The scripts are called with .sh but can be easily changed to ps1 if you are using Windows.

* Alternatively you can build the project in VS2022 with the latest .NET 6.0 SDK.
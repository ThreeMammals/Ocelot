Building
========

* You can also just run ``dotnet tool restore && dotnet cake`` locally! Output will got to the ``./artifacts`` directory.

* The best way to replicate the CI process is to build Ocelot locally is using the `Dockerfile.build <https://github.com/ThreeMammals/Ocelot/blob/main/docker/Dockerfile.build>`_ file
  which can be found in the `docker <https://github.com/ThreeMammals/Ocelot/tree/main/docker>`_ folder in `Ocelot <https://github.com/ThreeMammals/Ocelot/tree/main>`_ root.
  Use the following command ``docker build --platform linux/amd64 -f ./docker/Dockerfile.build .`` for example. You will need to change the platform flag depending on your platform.

* There is a `Makefile <https://github.com/ThreeMammals/Ocelot/blob/main/docs/Makefile>`_ to make it easier to call the various targets in `build.cake <https://github.com/ThreeMammals/Ocelot/blob/main/build.cake>`_.
  The scripts are called with **.sh** but can be easily changed to **.ps1** if you are using Windows.

* Alternatively you can build the project in VS2022 with the latest `.NET 8.0 <https://dotnet.microsoft.com/en-us/download/dotnet/8.0>`_ SDK.

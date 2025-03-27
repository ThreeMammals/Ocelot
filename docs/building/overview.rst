Overview
========

This document summarises the build and release process for the project.
The build scripts are written using `Cake <https://cakebuild.net/>`_, and they are defined in ``./build.cake``.
The scripts have been designed to be run by either developers locally or by a build server (currently `CircleCi <https://circleci.com/>`_), with minimal logic defined in the build server itself.

Building
--------

* You can also just run ``dotnet tool restore && dotnet cake`` locally! Output will got to the ``./artifacts`` directory.

* The best way to replicate the CI process is to build Ocelot locally is using the `Dockerfile.build <https://github.com/ThreeMammals/Ocelot/blob/main/docker/Dockerfile.build>`_ file
  which can be found in the `docker <https://github.com/ThreeMammals/Ocelot/tree/main/docker>`_ folder in `Ocelot <https://github.com/ThreeMammals/Ocelot/tree/main>`_ root.
  Use the following command ``docker build --platform linux/amd64 -f ./docker/Dockerfile.build .`` for example. You will need to change the platform flag depending on your platform.

* There is a `Makefile <https://github.com/ThreeMammals/Ocelot/blob/main/docs/Makefile>`_ to make it easier to call the various targets in `build.cake <https://github.com/ThreeMammals/Ocelot/blob/main/build.cake>`_.
  The scripts are called with **.sh** but can be easily changed to **.ps1** if you are using Windows.

* Alternatively you can build the project in VS2022 with the latest `.NET 8.0 <https://dotnet.microsoft.com/en-us/download/dotnet/8.0>`_ SDK.

Testing
-------

The tests should all just run and work as part of the build process. You can of course also run them in Visual Studio.

Create SSL certificate
^^^^^^^^^^^^^^^^^^^^^^

To create a certificate for *testing* we can do this via `OpenSSL <https://www.openssl.org/>`_:

* Install `openssl package <https://github.com/openssl/openssl>`_ (if you are using Windows, download binaries `here <https://www.openssl.org/source/>`_).
* Generate private key: ``openssl genrsa 2048 > private.pem``
* Generate the self-signed certificate: ``openssl req -x509 -days 1000 -new -key private.pem -out public.pem``
* If needed, create PFX: ``openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx``

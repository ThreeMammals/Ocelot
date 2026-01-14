.. role:: htm(raw)
  :format: html
.. role:: pdf(raw)
  :format: latex pdflatex
.. _Ocelot: https://github.com/ThreeMammals/Ocelot
.. _Cake: https://cakebuild.net
.. _Bash: https://www.gnu.org/software/bash
.. _build.cake: https://github.com/ThreeMammals/Ocelot/blob/main/build.cake
.. _GitHub Actions: https://docs.github.com/en/actions
.. _NuGet: https://www.nuget.org/profiles/ThreeMammals

Building
========

This document summarises the build and release process for the `Ocelot`_ project.
The build scripts are written using `Cake`_ (C# Make), with relevant build tasks defined in the '`build.cake`_' file located in the root of the `Ocelot`_ project.
The scripts are designed to be run by developers locally in a `Bash`_ terminal (on any OS), in Command Prompt (CMD) or PowerShell consoles (on Windows OS),
or by a CI/CD server (currently `GitHub Actions`_), with minimal logic defined in the build server itself.

The final goal of the build process is to create ``Ocelot.*`` `NuGet`_ packages (.nupkg files) for redistribution via the `NuGet`_ repository or manually.
The build process consists of several steps: (1) compilation, (2) testing, (3) creating and publishing `NuGet`_ packages, and (4) making an official GitHub release.
The build process requires pre-installed .NET SDKs on the build machine (host) for all target framework monikers: TFMs are ``net8.0`` and ``net9.0`` currently.
In general, the build process is the same across all environments and tools, with a few differences described below.

.. _b-in-ide:

In IDE
------
.. _Release configuration: https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-set-debug-and-release-configurations?view=vs-2022

In an IDE, a DevOps engineer can build the project in Visual Studio IDE or another IDE in `Release configuration`_ mode, but the latest .NET 8/9 SDKs must be pre-installed on the local machine.
However, this approach is not practical because the generated '.nupkg' files must be uploaded to `NuGet`_ manually, and the GitHub release must also be created manually.
A better approach is to utilize the '`build.cake`_' script :ref:`b-in-terminal`, which covers all building scenarios.

.. _b-in-terminal:

In terminal
-----------
.. _./: https://github.com/ThreeMammals/Ocelot/tree/main/

  Folder: `./`_

These are local machine or remote server building scenarios using build scripts, aka '`build.cake`_'.
In these scenarios, the following two commands should be run in a terminal from the project's root folder:

.. code-block:: shell

  dotnet tool restore && dotnet cake  # In Bash terminal
  dotnet tool restore; dotnet cake  # In PowerShell terminal

.. _break: http://break.do

  **Note**: The default target task ("Default") is "Build", and output files will be stored in the ``./artifacts`` directory.

To run a desired target task, you need to specify its *name*:

.. code-block:: shell

  dotnet tool restore && dotnet cake --target=name  # In Bash terminal
  dotnet tool restore; dotnet cake --target=name  # In PowerShell terminal

For example,

- .. code-block:: shell

    dotnet cake --target=Build

  It runs a local build, performing compilation and testing only.

- .. code-block:: shell

    dotnet cake --target=Version
  
  It checks the next version to be tagged in the Git repository during the next release, without performing compilation or testing tasks.

- .. code-block:: shell

    dotnet cake --target=CreateReleaseNotes
  
  It generates Release Notes artifacts in the ``/artifacts/Packages`` folder using the ``ReleaseNotes.md`` template file.

- .. code-block:: shell

    dotnet cake --target=Release

  It creates a release, consisting of the following steps: compilation, testing, generating release notes, creating .nupkg files, publishing `NuGet`_ packages, and finally, making a GitHub release.

.. _dotnet-tools.json: https://github.com/ThreeMammals/Ocelot/blob/main/.config/dotnet-tools.json

  **Note 1**: The building tools for the ``dotnet tool restore`` command are configured in the `dotnet-tools.json`_ file.

  **Note 2**: Some targets (build tasks) require appropriate environment variables to be defined directly in the terminal session (aka secret tokens).

.. _b-with-docker:

With Docker
-----------
.. _docker: https://github.com/ThreeMammals/Ocelot/tree/main/docker
.. _Dockerfile.build: https://github.com/ThreeMammals/Ocelot/blob/main/docker/Dockerfile.build
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0

  Folder: ./`docker`_

The best way to replicate the CI/CD process and build `Ocelot`_ locally is by using the `Dockerfile.build`_ file, which can be found in the '`docker`_' folder in the `Ocelot`_ root directory.
For example, use the following command:

.. code-block:: shell

  docker build --platform linux/amd64 -f ./docker/Dockerfile.build .

You may need to adjust the platform flag depending on your system.

  **Note**: This approach is somewhat excessive, but it will work if you are a masterful Docker user. 🙂
  The Ocelot team has not followed this approach since version `24.0`_, favoring :ref:`b-with-ci-cd`-based builds and occasionally building :ref:`b-in-terminal` instead.

.. _b-with-ci-cd:

With CI/CD
----------
.. _workflows: https://github.com/ThreeMammals/Ocelot/tree/main/.github/workflows 
.. _PR: https://github.com/ThreeMammals/Ocelot/actions/workflows/pr.yml
.. _Develop: https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml
.. _Release: https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml
.. _Coveralls: https://coveralls.io
.. |ReleaseButton| image:: https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml/badge.svg
  :target: https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml
  :alt: Release Status
  :class: img-valign-textbottom
.. |DevelopButton| image:: https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml/badge.svg
  :target: https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml
  :alt: Development Status
  :class: img-valign-textbottom
.. |DevelopCoveralls| image:: https://coveralls.io/repos/github/ThreeMammals/Ocelot/badge.svg?branch=develop
  :target: https://coveralls.io/github/ThreeMammals/Ocelot?branch=develop
  :alt: Coveralls Status
  :class: img-valign-textbottom
.. |ReleaseCoveralls| image:: https://coveralls.io/repos/github/ThreeMammals/Ocelot/badge.svg?branch=main
  :target: https://coveralls.io/github/ThreeMammals/Ocelot?branch=main
  :alt: Coveralls Status
  :class: img-valign-textbottom
.. _break2: http://break.do

  | Folder: ./.github/`workflows`_
  | Provider: `GitHub Actions`_
  | Workflows: `PR`_, `Develop`_, `Release`_
  | Dashboard: `Workflow runs <https://github.com/ThreeMammals/Ocelot/actions>`_ (Actions tab)

The `Ocelot`_ project utilizes `GitHub Actions`_ as a CI/CD provider, offering seamless integrations with the GitHub ecosystem and APIs.
Starting from version `24.0`_, all pull requests, development commits, and releases are built using `GitHub Actions`_ workflows.
There are three `workflows`_: one for pull requests (`PR`_), one for the ``develop`` branch (`Develop`_), and one for the ``main`` branch (`Release`_).

  **Note**: Each workflow has a dedicated status badge in the `Ocelot README`_:
  the |ReleaseButton|:pdf:`\href{https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml}{Release}` button and
  the |DevelopButton|:pdf:`\href{https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml}{Develop}` button,
  with the `PR`_ status being published directly in a pull request under the "Checks" tab.

The `PR`_ workflow will track code coverage using `Coveralls`_.
After opening a pull request or submitting a new commit to a pull request, `Coveralls`_ will publish a short message with the current code coverage once the top commit is built.
Considering that `Coveralls`_ retains the entire history but does not fail the build if coverage falls below the threshold, all workflows have a built-in 80% threshold,
applied internally within the ``build-cake`` job, particularly during the "`Cake Build`_" step-action.
If the code coverage of a newly opened pull request drops below the 80% threshold, the `'build-cake' job`_ will fail, logging an appropriate message in the "`Cake Build`_" step.

  **Note 1**: There are special code coverage badges in `Ocelot README`_: the `Develop`_ |DevelopCoveralls| button and the `Release`_ |ReleaseCoveralls| button.

  **Note 2**: The current code coverage of the `Ocelot`_ project is around 85-86%. The coverage threshold is subject to change in upcoming releases.
  All `Coveralls`_ builds can be viewed by navigating to the `ThreeMammals/Ocelot <https://coveralls.io/github/ThreeMammals/Ocelot>`_ project on Coveralls.io.

Documentation
-------------
.. _docs: https://github.com/ThreeMammals/Ocelot/tree/main/docs
.. _.readthedocs.yaml: https://github.com/ThreeMammals/Ocelot/blob/main/.readthedocs.yaml
.. _Read the Docs: https://about.readthedocs.com
.. _Ocelot app: https://app.readthedocs.org/projects/ocelot/
.. _README: https://github.com/ThreeMammals/Ocelot/blob/main/docs/readme.md
.. _Ocelot README: https://github.com/ThreeMammals/Ocelot/blob/main/README.md
.. |ReleaseDocs| image:: https://readthedocs.org/projects/ocelot/badge/?version=latest&style=flat-square
  :target: https://app.readthedocs.org/projects/ocelot/builds/?version__slug=latest
  :alt: ReadTheDocs Status
  :class: img-valign-middle
.. |DevelopDocs| image:: https://readthedocs.org/projects/ocelot/badge/?version=develop&style=flat-square
  :target: https://app.readthedocs.org/projects/ocelot/builds/?version__slug=develop
  :alt: ReadTheDocs Status
  :class: img-valign-middle
.. _break3: http://break.do

  | Folder: ./`docs`_
  | Dashboard: `Ocelot app`_ project

Documentation building is configured using the '`.readthedocs.yaml`_' integration file, which allows builds to run separately via the `Read the Docs`_ publisher.
All build artifacts and document sources are located in the '`docs`_' folder.
More details on the documentation build process can be found in the `README`_.

  **Note 1**: Documentation builds have a dedicated status badges in `Ocelot README`_: the `Develop`_ |DevelopDocs| button and the `Release`_ |ReleaseDocs| button.

  **Note**: Documentation can be easily built locally in a terminal from the '`docs`_' folder by running the ``make.sh`` or ``make.bat`` scripts.
  The resulting documentation build files will be located in the ``./docs/_build`` folder, with the HTML documentation specifically written to the ``./docs/_build/html`` folder.

.. _b-testing:

Testing
-------

The tests should run and function correctly as part of the *building* process using the ``dotnet test`` command.
You can also run them in Visual Studio IDE within the Test Explorer window.
Depending on your build scenario, `Ocelot`_ *testing* can be performed as follows.

:ref:`b-in-ide`: Simply run tests via the Test Explorer window of Visual Studio IDE.

:ref:`b-in-terminal`: There are two main approaches:

1. Run the ``dotnet test`` command to perform all tests (unit, integration, and acceptance):

   .. code-block:: shell

      dotnet test -f net8.0 ./Ocelot.sln

   Or run tests separately per project:

   .. code-block:: shell

      dotnet test -f net8.0 ./test/Ocelot.UnitTests/Ocelot.UnitTests.csproj  # Unit tests only
      dotnet test -f net8.0 ./test/Ocelot.IntegrationTests/Ocelot.IntegrationTests.csproj  # Integration tests only
      dotnet test -f net8.0 ./test/Ocelot.AcceptanceTests/Ocelot.AcceptanceTests.csproj  # Acceptance tests only

2. Run ``dotnet cake`` command: ``dotnet cake --target=Tests`` to perform all tests (unit, integration and acceptance).
   Or run tests separately per *testing* project:

   .. code-block:: shell

      dotnet cake --target=UnitTests # unit tests only
      dotnet cake --target=IntegrationTests # integration tests only
      dotnet cake --target=AcceptanceTests # acceptance tests only

:ref:`b-with-docker`: This approach is not recommended.
Instead, perform automated testing :ref:`b-with-ci-cd` or opt for :ref:`b-in-terminal`-based testing, which is a more advanced method.

:ref:`b-with-ci-cd`: In `GitHub Actions`_ `workflows`_, the *testing* process consists of separate testing steps, organized per job:

* In the `'build' job`_: There are '`Unit Tests`_', '`Integration Tests`_', and '`Acceptance Tests`_' steps.
* In the `'build-cake' job`_: There is a '`Cake Build`_' step responsible for performing tests internally.

.. _'build' job: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+build%3A+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code
.. _Unit Tests: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22Unit+Tests%22+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code
.. _Integration Tests: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22Integration+Tests%22+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code
.. _Acceptance Tests: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22Acceptance+Tests%22+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code
.. _'build-cake' job: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22-cake%3A%22+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code
.. _Cake Build: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22cake-build%2F%22+path%3A%2F%5E%5C.github%5C%2Fworkflows%5C%2F%2F&type=code

SSL certificate
---------------

To create a certificate for :ref:`b-testing`, you can use `OpenSSL <https://www.openssl.org/>`_:

* Install the `openssl <https://github.com/openssl/openssl>`__ package (if you are using Windows, download the binaries `here <https://www.openssl.org/source/>`_).
* Generate a private key:

  .. code-block:: bash

    openssl genrsa 2048 > private.pem

* Generate a self-signed certificate:

  .. code-block:: bash

    openssl req -x509 -days 1000 -new -key private.pem -out public.pem

* If needed, create a PFX file:

  .. code-block:: bash

    openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx


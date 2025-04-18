Hosting Gotchas
===============

    Microsoft Learn: `Web server implementations in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/>`_

Many errors and incidents (gotchas) are related to web server hosting scenarios.
Please review deployment and web hosting common user scenarios below depending on your web server.

.. _hosting-gotchas-iis:

IIS
---

    | Repository Label: |image-IIS| `IIS <https://github.com/ThreeMammals/Ocelot/labels/IIS>`_
    | Microsoft Learn: `Host ASP.NET Core on Windows with IIS <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/>`_

We **do not** recommend to deploy Ocelot app to IIS environments, but if you do, keep in mind the gotchas below.

* When using ASP.NET Core 2.2+ and you want to use In-Process hosting, replace ``UseIISIntegration()`` with ``UseIIS()``, otherwise you will get startup errors.

* Make sure you use Out-of-process hosting model instead of In-process one
  (see `Out-of-process hosting with IIS and ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/out-of-process-hosting>`_),
  otherwise you will get very slow responses (see `1657`_).

* Ensure all DNS servers of all downstream hosts are online and they function perfectly, otherwise you will get slow responses (see `1630`_).

The community constanly reports `issues related to IIS <https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+IIS>`_.
If you have some troubles in IIS environment to host Ocelot app, first of all, read open/closed issues, and after that, search for `IIS-related objects`_ in the repository.
Probably you will find a ready solution by Ocelot community members. 

    Finally, we have special label |image-IIS| for all `IIS-related objects`_.
    Feel free to put this label onto `issues <https://github.com/ThreeMammals/Ocelot/labels/IIS>`_, `pull requests <https://github.com/ThreeMammals/Ocelot/pulls?q=is%3Apr+label%3AIIS+>`_, `discussions <https://github.com/ThreeMammals/Ocelot/discussions?discussions_q=label%3AIIS>`_, etc.

.. |image-IIS| image:: ../images/label-IIS-c5def5.svg
  :alt: label IIS
  :target: https://github.com/ThreeMammals/Ocelot/labels/IIS
.. _IIS-related objects: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20IIS&type=code

.. _hosting-gotchas-kestrel:

Kestrel
-------

    | Repository Label: |image-Kestrel| `Kestrel <https://github.com/ThreeMammals/Ocelot/labels/Kestrel>`_
    | Microsoft Learn: `Kestrel web server in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel>`_

We **do** recommend to deploy Ocelot app to self-hosting environments, aka Kestrel vs Docker.
We try to optimize Ocelot web app for Kestrel & Docker hosting scenarios, but keep in mind the following gotchas.

**1. Upload and download large files** [#f1]_

This is proxying the large content through the gateway: when you pump large (static) files using the gateway.
We believe that your client apps should have direct integration to (static) files persistent storages and services: remote & destributed file systems, CDNs, static files & blob storages, etc.
We **do not** recommend to pump large files (100Mb+ or even larger 1GB+) using gateway because of performance reasons: consuming memory and CPU, long delay times, producing network errors for downstream streaming, impact on other routes.

  | The community constanly reports issues related to `large files <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22large+file%22&type=issues>`_, ``application/octet-stream`` content type, :ref:`chunked-encoding`, etc., see issues `749`_, `1472`_.
  | If you still want to pump large files through an Ocelot gateway instance, use `23.0`_ version and higher.
  | In case of some errors, see the next point.

**2. Maximum request body size**

    Docs: `Maximum request body size | Configure options for the ASP.NET Core Kestrel web server <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options#maximum-request-body-size>`_.

ASP.NET ``HttpRequest`` behaves erroneously for application instances that do not have their Kestrel `MaxRequestBodySize <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits.maxrequestbodysize>`_ option configured correctly and having pumped large files of unpredictable size which exceeds the limit.

As a quick fix, use this configuration recipe:

.. code-block:: csharp

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel((context, serverOptions) =>
    {
        int myVideoFileMaxSize = 1_073_741_824; // assume your file storage has max file size as 1 GB (1_073_741_824)
        int totalSize = myVideoFileMaxSize + 26_258_176; // and add some extra size
        serverOptions.Limits.MaxRequestBodySize = totalSize; // 1_100_000_000 thus 1 GB file should not exceed the limit
    });

.. _break: http://break.do

    Finally, we have special label |image-Kestrel| for all `Kestrel-related objects <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20Kestrel&type=code>`_.
    Feel free to put this label onto `issues <https://github.com/ThreeMammals/Ocelot/labels/Kestrel>`__, `pull requests <https://github.com/ThreeMammals/Ocelot/pulls?q=is%3Apr+label%3AKestrel+>`__, `discussions <https://github.com/ThreeMammals/Ocelot/discussions?discussions_q=label%3AKestrel>`__, etc.

.. |image-Kestrel| image:: ../images/label-Kestrel-c5def5.svg
  :alt: label Kestrel
  :target: https://github.com/ThreeMammals/Ocelot/labels/Kestrel

""""

.. [#f1] Large files pumping is stabilized and available as complete solution starting in `23.0`_ release. We believe our PRs `1724`_, `1769`_ helped to resolve the issues and stabilize large content proxying problems of `22.0.1`_ version and lower.
.. _22.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/22.0.1
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _749: https://github.com/ThreeMammals/Ocelot/issues/749
.. _1472: https://github.com/ThreeMammals/Ocelot/issues/1472
.. _1657: https://github.com/ThreeMammals/Ocelot/issues/1657
.. _1630: https://github.com/ThreeMammals/Ocelot/issues/1630
.. _1724: https://github.com/ThreeMammals/Ocelot/pull/1724
.. _1769: https://github.com/ThreeMammals/Ocelot/pull/1769

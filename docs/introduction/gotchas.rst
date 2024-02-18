Gotchas
=============

Many errors and incidents (gotchas) are related to web server hosting scenarios.
Please review deployment and web hosting common user scenarios below depending on your web server.

IIS
---

    Microsoft Learn: `Host ASP.NET Core on Windows with IIS <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/>`_

We **do not** recommend to deploy Ocelot app to IIS environments, but if you do, keep in mind the gotchas below.

* When using ASP.NET Core 2.2+ and you want to use In-Process hosting, replace ``UseIISIntegration()`` with ``UseIIS()``, otherwise you will get startup errors.

* Make sure you use Out-of-process hosting model instead of In-process one
  (see `Out-of-process hosting with IIS and ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/out-of-process-hosting>`_),
  otherwise you will get very slow responses (see `1657 <https://github.com/ThreeMammals/Ocelot/issues/1657>`_).

* Ensure all DNS servers of all downstream hosts are online and they function perfectly, otherwise you will get slow responses (see `1630 <https://github.com/ThreeMammals/Ocelot/issues/1630>`_).

The community constanly reports `issues related to IIS <https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+IIS>`_.
If you have some troubles in IIS environment to host Ocelot app, first of all, read open/closed issues, and after that, search for `IIS <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20IIS&type=code>`_ in the repository.
Probably you will find a ready solution by Ocelot community members. 

Finally, we have special label |IIS| for all IIS related objects. Feel free to put this label onto issues, PRs, discussions, etc.

.. |IIS| image:: https://img.shields.io/badge/-IIS-c5def5.svg

Kestrel
-------

    Microsoft Learn: `Kestrel web server in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel>`_

We **do** recommend to deploy Ocelot app to self-hosting environments, aka Kestrel vs Docker.
We try to optimize Ocelot web app for Kestrel & Docker hosting scenarios, but keep in mind the following gotchas.

* **Upload and download large files** [#f1]_, proxying the content through the gateway. It is strange when you pump large (static) files using the gateway.
  We believe that your client apps should have direct integration to (static) files persistent storages and services: remote & destributed file systems, CDNs, static files & blob storages, etc.
  We **do not** recommend to pump large files (100Mb+ or even larger 1GB+) using gateway because of performance reasons: consuming memory and CPU, long delay times, producing network errors for downstream streaming, impact on other routes.

  | The community constanly reports issues related to `large files <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22large+file%22&type=issues>`_, ``application/octet-stream`` content type, :ref:`chunked-encoding`, etc., see issues `749 <https://github.com/ThreeMammals/Ocelot/issues/749>`_, `1472 <https://github.com/ThreeMammals/Ocelot/issues/1472>`_.
  | If you still want to pump large files through an Ocelot gateway instance, use `23.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0>`_ version and higher [#f1]_.
  | In case of some errors, see the next point.

* **Maximum request body size**. ASP.NET ``HttpRequest`` behaves erroneously for application instances that do not have their Kestrel `MaxRequestBodySize <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits.maxrequestbodysize>`_ option configured correctly and having pumped large files of unpredictable size which exceeds the limit.

  | Please review these docs: `Maximum request body size | Configure options for the ASP.NET Core Kestrel web server <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options#maximum-request-body-size>`_.

  | As a quick fix, use this configuration recipe:

  .. code-block:: csharp

      builder.WebHost.ConfigureKestrel((context, serverOptions) =>
      {
          int myVideoFileMaxSize = 1_073_741_824; // assume your file storage has max file size as 1 GB (1_073_741_824)
          int totalSize = myVideoFileMaxSize + 26_258_176; // and add some extra size
          serverOptions.Limits.MaxRequestBodySize = totalSize; // 1_100_000_000 thus 1 GB file should not exceed the limit
      });

  Hope it helps.


""""

.. [#f1] Large files pumping is stabilized and available as complete solution starting in `23.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0>`__ release. We believe our PRs `1724 <https://github.com/ThreeMammals/Ocelot/pull/1724>`_, `1769 <https://github.com/ThreeMammals/Ocelot/pull/1769>`_ helped to resolve the issues and stabilize large content proxying problems of `22.0.1 <https://github.com/ThreeMammals/Ocelot/releases/tag/22.0.1>`_ version and lower.

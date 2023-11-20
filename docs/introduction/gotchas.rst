Gotchas
=============
	
IIS
-----

    Microsoft Learn: `Host ASP.NET Core on Windows with IIS <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-7.0>`_

We do not recommend to deploy Ocelot app to IIS environments, but if you do, keep in mind the gotchas below.

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

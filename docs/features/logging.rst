Logging
=======

Ocelot uses the standard logging interfaces ``ILoggerFactory`` and ``ILogger<T>`` at the moment.
This is encapsulated in ``IOcelotLogger`` and ``IOcelotLoggerFactory`` with an implementation for the standard `ASP.NET Core logging <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/>`_ stuff at the moment.
This is because Ocelot adds some extra info to the logs such as **RequestId** if it is configured.

There is a global `error handler middleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20ExceptionHandlerMiddleware&type=code>`_ that should catch any exceptions thrown and log them as errors.

Finally, if logging is set to **Trace** level, Ocelot will log starting, finishing and any middlewares that throw an exception which can be quite useful.

RequestId
---------

The reason for not just using `bog standard <https://notoneoffbritishisms.com/2015/03/27/bog-standard/>`_ framework logging is that
we could not work out how to override the **RequestId** that get's logged when setting **IncludeScopes** to ``true`` for logging settings.
TODO ``Nicely onto the next feature.``

TODO ``Describe these props`` -->>

* RequestId
* PreviousRequestId

Warning
-------

If you are logging to `Console <https://learn.microsoft.com/en-us/dotnet/api/system.console>`_, you will get terrible performance.
The team has had so many issues about performance issues with Ocelot and it is always logging level **Debug**, logging to `Console <https://learn.microsoft.com/en-us/dotnet/api/system.console>`_.

* **Warning!** Make sure you are logging to something proper in production environment!
* Use **Error** and **Critical** levels in production environment!
* Use **Warning** level in testing environment!

These and other recommendations are below in the **Best Practices** section.

Best Practices
--------------
* https://github.com/ThreeMammals/Ocelot/pull/1745#issuecomment-1792210250 A link and quote for custom logging provider. And our OcelotLogger is the custom logger, right?
* How to switch off logging at production to get top performance
* We can pay attention to Warning sections. Seems this section remains the same, because I wrote it for v.20 release
* A couple of samples as code block should be added to explain user how to configure MS Logger and Serilog logger for example

Performance Review
------------------
In v22 PR 1745 performance of Logging was improved.

Indicators? Screenshots?

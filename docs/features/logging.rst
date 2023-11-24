Logging
=======

Ocelot uses the standard logging interfaces ``ILoggerFactory`` and ``ILogger<T>`` at the moment.
This is encapsulated in ``IOcelotLogger`` and ``IOcelotLoggerFactory`` with an implementation for the standard `ASP.NET Core logging <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/>`_ stuff at the moment.
This is because Ocelot adds some extra info to the logs such as **request ID** if it is configured.

There is a global `error handler middleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20ExceptionHandlerMiddleware&type=code>`_ that should catch any exceptions thrown and log them as errors.

Finally, if logging is set to **Trace** level, Ocelot will log starting, finishing and any middlewares that throw an exception which can be quite useful.

The reason for not just using `bog standard <https://notoneoffbritishisms.com/2015/03/27/bog-standard/>`_ framework logging is that
we could not work out how to override the request id that get's logged when setting **IncludeScopes** to ``true`` for logging settings.
Nicely onto the next feature.

Warning
-------

If you are logging to `Console <https://learn.microsoft.com/en-us/dotnet/api/system.console>`_, you will get terrible performance.
The team has had so many issues about performance issues with Ocelot and it is always logging level **Debug**, logging to `Console <https://learn.microsoft.com/en-us/dotnet/api/system.console>`_.

* **Warning!** Make sure you are logging to something proper in production environment!
* Use **Error** and **Critical** levels in production environment!
* Use **Warning** level in testing environment!

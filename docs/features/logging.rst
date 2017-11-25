Logging
=======

Ocelot uses the standard logging interfaces ILoggerFactory / ILogger<T> at the moment. 
This is encapsulated in  IOcelotLogger / IOcelotLoggerFactory with an implementation 
for the standard asp.net core logging stuff at the moment. 

There are a bunch of debugging logs in the ocelot middlewares however I think the 
system probably needs more logging in the code it calls into. Other than the debugging
there is a global error handler that should catch any errors thrown and log them as errors.

The reason for not just using bog standard framework logging is that I could not 
work out how to override the request id that get's logged when setting IncludeScopes 
to true for logging settings. Nicely onto the next feature.
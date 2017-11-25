Welcome to Ocelot
=================

This project is aimed at people using .NET running 
a micro services / service orientated architecture 
that need a unified point of entry into their system.

In particular I want easy integration with 
IdentityServer reference and bearer tokens. 

We have been unable to find this in my current workplace
without having to write our own Javascript middlewares 
to handle the IdentityServer reference tokens. We would
rather use the IdentityServer code that already exists
to do this.

Ocelot is a bunch of middlewares in a specific order.

Ocelot manipulates the HttpRequest object into a state specified by its configuration until 
it reaches a request builder middleware where it creates a HttpRequestMessage object which is 
used to make a request to a downstream service. The middleware that makes the request is 
the last thing in the Ocelot pipeline. It does not call the next middleware. 
The response from the downstream service is stored in a per request scoped repository 
and retrived as the requests goes back up the Ocelot pipeline. There is a piece of middleware 
that maps the HttpResponseMessage onto the HttpResponse object and that is returned to the client.
That is basically it with a bunch of other features.

.. toctree::
   :maxdepth: 2
   :hidden:
   :caption: Introduction

   introduction/bigpicture
   introduction/gettingstarted
   introduction/contributing
   introduction/notsupported
   
.. toctree::
   :maxdepth: 2
   :hidden:
   :caption: Features

   features/routing
   features/configuration
   features/servicediscovery
   features/authentication
   features/authorisation
   features/administration
   features/caching
   features/qualityofservice
   features/claimstransformation 
   features/logging
   features/requestid
   features/middlewareinjection
   
.. toctree::
   :maxdepth: 2
   :hidden:
   :caption: Building Ocelot

   building/overview
   building/building
   building/tests
   building/releaseprocess




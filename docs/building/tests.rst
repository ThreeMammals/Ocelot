Tests
=====

The tests should all just run and work apart from the integration tests which need the following 
environmental variables setting. This is a manual step at the moment.

    ``OCELOT_USERNAME=admin``

    ``OCELOT_HASH=kE/mxd1hO9h9Sl2VhGhwJUd9xZEv4NP6qXoN39nIqM4=``

    ``OCELOT_SALT=zzWITpnDximUNKYLiUam/w==``

On windows you can use..

    ``SETX OCELOT_USERNAME admin``

On mac..
    
    ``export OCELOT_USERNAME=admin``

I need to work out a nicer way of doing this in the future.




Not Supported
=============

Ocelot does not support...
	
* Chunked Encoding - Ocelot will always get the body size and return Content-Length header. Sorry if this doesn't work for your use case! 
	
* Fowarding a host header - The host header that you send to Ocelot will not be forwarded to the downstream service. Obviously this would break everything :(
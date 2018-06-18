Hosting
=======

Below are some notes around possible approaches to hosting Ocelot. I am happy to offer advice on best practices around setting up Ocelot and suggest alternative technologies.

I am going to go through some cloud providers and talk about how services they offer could be used to host Ocelot.

Notes
^^^^^

- Please raise an issue on GitHub with anything in here that is incorrect. I am not the all seeing eye most of this is written from memory.
- Kubernetes / K8s I will talk about this in more detail elsewhere as it is can be self hosted or provided by multiple cloud providers as a service.
- My Azure knowledge is a bit ropey, been a while since I used it.
- My GCP knowledge stops at kubernetes as a service so its not great.
- Ocelot is stateless (ish) unless you use CacheManager with in memory cache provider (dont do this in production anyway unless you understand the risks) and this makes it easy to distribute.
- Ocelot's configuration is easy to source control which is nice. Not sure how easy this is with certain cloud providers. You probably then need to use their CLI tools / more services to get a nice deployment pipeline. I'm not saying this is a bad thing just might be a tradeoff worth thinking about in a given context!
- If you are an enterprise and are returning data from Ocelot stick a CDN in front your lb / reverse proxy / webserver stack so your users get a better experience. Unless for some reason you can't cache at this level.

AWS
---

ElasticBeanstalk - This could be used to host Ocelot in a few ways. Either as a plain IIS type host or using Ocelot in a container with nginx in front of it (EB provides the nginx). You would point your ALB / ELB at the Ocelot instances in EB and these would route traffic to your services. I would not expose these services to www only Ocelot would be able to talk to them. I also would not expose Ocelot to www only via the ALB / ELB. You would then get all the nice things from EB and should easily be able to spin up more than 1 instance of Ocelot if you get lots of load. Distribute Ocelot in more than one region etc.

Lambda - Ocelot can be hosted as a lambda function. You would need AWS API gateway in front of it so for me this is pretty pointless....just use the AWS API gateway to talk to your services unless you REALLY need to use Ocelot's extra functionality like DelegatingHandlers, Identity Server reference tokens etc.

Elastic Container Service (not K8s as a service) - Much like EB it is easy enough to throw Ocelot into a docker container and push it into ECS then start routing traffic through it. The main problem is exposing it over www you are probably going to want an ALB / ELB over it and you need to work out how you can scale this. You might even need AWS API gateway which again means you probably shouldnt be using Ocelot.

Azure
-----

Web Apps - Much like AWS EB you would just throw your Ocelot web app into azure and start routing traffic to it. Ocelot could then proxy the requests onto whatever services you have in Azure. It has been a while since I used Azure so I'm not sure if you get a load balancer over a web app or if you get any kind of DR.

Functions - Much like AWS lambdas Ocelot can be run as a function. These can be exposed directly to www without having to use anything extra to trigger them if I remember correctly (whereas I believe AWS need API gateway to trigger them). However unlike lambdas is it a right faff to get these private. You would need to host them on an ASE which means you lose elastic scalability immediately....rendering the use of functions pointless. This is where AWS has a big advantage over MS at the moment imo. The lambdas are private by default and need exposing whereas Azure they are public when called over HTTP (I guess this kinda makes sense but they need a private over HTTP as well)

Service Fabric - There is more detail on hosting with Service Fabric in the big picture section (todo link to this).

GCP
---

App Engine - package Ocelot up as a .net core app and deploy into App Engine (pretty sure it is supported at the moment) expose over www and then have it route traffic wherever you need.

Container Engine - similar to the above package Ocelot into a container, throw it into gce and expose to www and route traffic with it.

Other Clouds
------------

There are loads of cloud providers know and I havent used them such as DigitalOcean, CloudFoundry or Alibaba. They all provide way you could host Ocelot. If anyone would like to document this stick a PR in GitHub! :)

On Prem / Co-lo / VMs....like classic hosting :)
------------------------------------------------

Ocelot can be hosted on Windows, Linux or Mac so you can have any physical or virtual host setup to run Ocelot and put it between your services and www. 

Typically thinking of a legacy type scenario where companies host their own infrastructure. You might have a load balancer and then expose your services via this. You would put Ocelot between your load balancer and your services. If you don't have multiple services Ocelot might not be a good tool to use.

If I was doing this myself I would put nginx in front of Ocelot use docker or a larger container orchestrator to manage your Ocelot deployment in a container behind nginx or with nginx and ocelot in a sidecar configuration. 

Other options would be to use IIS which is great but I prefer nginx. You would just deploy Ocelot into IIS like a typical website and then let it route traffic to your services. There is extra configuration required in asp.net to host under IIS so make sure you check this out.

Other options
^^^^^^^^^^^^^

NGINX - I'm pretty sure nginx can do pretty much everything that Ocelot (and any other API gateway) can do if you can be bothered to learn how to use the lua scripting in it, how to configure it and finally how to run it at scale. E.g. for nginx caching I think you need the enterprise version but I might be wrong on that been a while since I looked at it (If i am wrong please tell me on GitHub).

Kong / Tyk - Before I looked at making an API Gateway for .NET these were the two alternatives I looked at for the business I worked for at the time. I dismissed them both because I did not think that we could get IdentityServer reference tokens working with them. I since learned that Kong let's you stick any old js middleware into it's pipeline so in theory you could write code to handle this scenario here. Now wether that is a good idea or not I can't say. The IdentityServer team have already written the code in C# and it is probably a good idea to use theirs rather than write it yourself but its not that much code! Kong is much more popular than Tyk and Ocelot so I would use this as my API gateway unless I really had to use .NET / C# or there was some feature that Ocelot has that they don't. I guess the only thing that might be easier with Ocelot is the DelegatingHandlers stuff.

AWS API gateway - If you are hosting your services in AWS then this is a good choice. The only reason I would use Ocelot is if I needed to run my API gateway locally for development purposes easily, IdentityServer reference token support or some other Ocelot feature.

Azure API management - If you are hosting your services in Azure then this is a good choice. The only reason I would use Ocelot is if I needed to run my API gateway locally for development purposes easily, IdentityServer reference token support or some other Ocelot feature.

Apigee - I have never actually used this so I don't know if you can run it locally for dev purposes or if it can support all of Ocelot's feaures but it looks good from a quick read and I know someone who uses it and he is happy :)

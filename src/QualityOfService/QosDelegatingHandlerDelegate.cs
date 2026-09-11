using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;

namespace Ocelot.QualityOfService;

public delegate DelegatingHandler QosDelegatingHandlerDelegate(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory);

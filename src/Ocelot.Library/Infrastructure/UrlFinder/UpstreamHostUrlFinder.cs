using System;
using System.Collections.Generic;
using Ocelot.Library.Infrastructure.HostUrlRepository;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlFinder
{
    public class UpstreamHostUrlFinder : IUpstreamHostUrlFinder
    {
        private readonly IHostUrlMapRepository _hostUrlMapRepository;

        public UpstreamHostUrlFinder(IHostUrlMapRepository hostUrlMapRepository)
        {
            _hostUrlMapRepository = hostUrlMapRepository;
        }
        public Response<string> FindUpstreamHostUrl(string downstreamBaseUrl)
        {                                           
            var baseUrl = _hostUrlMapRepository.GetBaseUrlMap(downstreamBaseUrl);

            if(baseUrl.IsError) 
            {
                return new ErrorResponse<string>(new List<Error> {new UnableToFindUpstreamHostUrl()});
            }

            return new OkResponse<string>(baseUrl.Data.UpstreamHostUrl);
        }
    }
}
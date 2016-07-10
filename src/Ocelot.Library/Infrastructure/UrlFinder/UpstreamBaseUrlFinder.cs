using System;
using Ocelot.Library.Infrastructure.BaseUrlRepository;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlFinder
{
    public class UpstreamBaseUrlFinder : IUpstreamBaseUrlFinder
    {
        private readonly IBaseUrlMapRepository _baseUrlMapRepository;

        public UpstreamBaseUrlFinder(IBaseUrlMapRepository baseUrlMapRepository)
        {
            _baseUrlMapRepository = baseUrlMapRepository;
        }
        public Response<string> FindUpstreamBaseUrl(string downstreamBaseUrl)
        {                                            
            var baseUrl = _baseUrlMapRepository.GetBaseUrlMap(downstreamBaseUrl);

            return new OkResponse<string>(baseUrl.Data.UpstreamBaseUrl);
        }
    }
}
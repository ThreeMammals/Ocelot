using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateRepository
{
    public class InMemoryUrlTemplateMapRepository : IUrlTemplateMapRepository
    { 
        private readonly Dictionary<string, string> _urlTemplates;
        public InMemoryUrlTemplateMapRepository()
        {
            _urlTemplates = new Dictionary<string,string>();
        }

        public Response<List<UrlTemplateMap>> All
        {
            get
            {
                var routes =  _urlTemplates
                .Select(r => new UrlTemplateMap(r.Key, r.Value))
                .ToList();
                return new OkResponse<List<UrlTemplateMap>>(routes);
            }
        }

        public Response AddUrlTemplateMap(UrlTemplateMap urlMap)
        {
            if(_urlTemplates.ContainsKey(urlMap.DownstreamUrlTemplate))
            {
                return new ErrorResponse(new List<Error>(){new DownstreamUrlTemplateAlreadyExists()});
            }

            _urlTemplates.Add(urlMap.DownstreamUrlTemplate, urlMap.UpstreamUrlPathTemplate);

            return new OkResponse();
        }
    } 
}
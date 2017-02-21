using System;
using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator
{
    public class UrlBuilder : IUrlBuilder
    {
        public Response<DownstreamUrl> Build(string downstreamPath, string downstreamScheme, HostAndPort downstreamHostAndPort)
        {
            if (string.IsNullOrEmpty(downstreamPath))
            {
                return new ErrorResponse<DownstreamUrl>(new List<Error> {new DownstreamPathNullOrEmptyError()});
            }

            if (string.IsNullOrEmpty(downstreamScheme))
            {
                return new ErrorResponse<DownstreamUrl>(new List<Error> { new DownstreamSchemeNullOrEmptyError() });
            }

            if (string.IsNullOrEmpty(downstreamHostAndPort.DownstreamHost))
            {
                return new ErrorResponse<DownstreamUrl>(new List<Error> { new DownstreamHostNullOrEmptyError() });
            }

            var builder = new UriBuilder
            {
                Host = downstreamHostAndPort.DownstreamHost,
                Path = downstreamPath,
                Scheme = downstreamScheme
            };

            if (downstreamHostAndPort.DownstreamPort > 0)
            {
                builder.Port = downstreamHostAndPort.DownstreamPort;
            }
            
            var url = builder.Uri.ToString();

            return new OkResponse<DownstreamUrl>(new DownstreamUrl(url));
        }
    }
}
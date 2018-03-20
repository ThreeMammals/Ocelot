﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;
using System.Net.Http;
using System;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace Ocelot.QueryStrings
{
    public class AddQueriesToRequest : IAddQueriesToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddQueriesToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response SetQueriesOnDownstreamRequest(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims, HttpRequestMessage downstreamRequest)
        {
            var queryDictionary = ConvertQueryStringToDictionary(downstreamRequest.RequestUri.Query);

            foreach (var config in claimsToThings)
            {
                var value = _claimsParser.GetValue(claims, config.NewKey, config.Delimiter, config.Index);

                if (value.IsError)
                {
                    return new ErrorResponse(value.Errors);
                }

                var exists = queryDictionary.FirstOrDefault(x => x.Key == config.ExistingKey);

                if (!string.IsNullOrEmpty(exists.Key))
                {
                    queryDictionary[exists.Key] = value.Data;
                }
                else
                {
                    queryDictionary.Add(config.ExistingKey, value.Data);
                }
            }

            var uriBuilder = new UriBuilder(downstreamRequest.RequestUri);

            uriBuilder.Query = ConvertDictionaryToQueryString(queryDictionary);

            downstreamRequest.RequestUri = uriBuilder.Uri;

            return new OkResponse();
        }

        private Dictionary<string, StringValues> ConvertQueryStringToDictionary(string queryString)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers
                .ParseQuery(queryString);

            return query;
        }

        private string ConvertDictionaryToQueryString(Dictionary<string, StringValues> queryDictionary)
        {
            var builder = new StringBuilder();

            builder.Append("?");
            
            int outerCount = 0;

            foreach (var query in queryDictionary)
            {
                for (int innerCount = 0; innerCount < query.Value.Count; innerCount++)
                {
                    builder.Append($"{query.Key}={query.Value[innerCount]}");

                    if(innerCount < (query.Value.Count - 1))
                    {
                        builder.Append("&");
                    }
                }

                if(outerCount < (queryDictionary.Count - 1))
                {
                    builder.Append("&");
                }

                outerCount++;
            }

            return builder.ToString();
        }
    }
}
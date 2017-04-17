using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;
using System.Net.Http;
using System;

namespace Ocelot.QueryStrings
{
    public class AddQueriesToRequest : IAddQueriesToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddQueriesToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        //public Response SetQueriesOnContext(List<ClaimToThing> claimsToThings, HttpContext context)
        //{
        //    var queryDictionary = ConvertQueryStringToDictionary(context.Request.QueryString);

        //    foreach (var config in claimsToThings)
        //    {
        //        var value = _claimsParser.GetValue(context.User.Claims, config.NewKey, config.Delimiter, config.Index);

        //        if (value.IsError)
        //        {
        //            return new ErrorResponse(value.Errors);
        //        }

        //        var exists = queryDictionary.FirstOrDefault(x => x.Key == config.ExistingKey);

        //        if (!string.IsNullOrEmpty(exists.Key))
        //        {
        //            queryDictionary[exists.Key] = value.Data;
        //        }
        //        else
        //        {
        //            queryDictionary.Add(config.ExistingKey, value.Data);
        //        }
        //    }

        //    context.Request.QueryString = ConvertDictionaryToQueryString(queryDictionary);

        //    return new OkResponse();
        //}

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

        //private Dictionary<string, string> ConvertQueryStringToDictionary(HttpContext context)
        //{
        //    return Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(context.Request.QueryString.Value)
        //        .ToDictionary(q => q.Key, q => q.Value.FirstOrDefault() ?? string.Empty);
        //}

        private Dictionary<string, string> ConvertQueryStringToDictionary(string queryString)
        {
            return Microsoft.AspNetCore.WebUtilities.QueryHelpers
                .ParseQuery(queryString)
                .ToDictionary(q => q.Key, q => q.Value.FirstOrDefault() ?? string.Empty);
        }

        //private Microsoft.AspNetCore.Http.QueryString ConvertDictionaryToQueryString(Dictionary<string, string> queryDictionary)
        //{
        //    var newQueryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", queryDictionary);

        //    return new Microsoft.AspNetCore.Http.QueryString(newQueryString);
        //}

        private string ConvertDictionaryToQueryString(Dictionary<string, string> queryDictionary)
        {
            return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", queryDictionary);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Authentication;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;

namespace Ocelot.Raft
{
    [ExcludeFromCoverage]
    public class HttpPeer : IPeer
    {
        private string _hostAndPort;
        private HttpClient _httpClient;
        private JsonSerializerSettings _jsonSerializerSettings;
        private string _baseSchemeUrlAndPort;
        private BearerToken _token;
        private IOcelotConfiguration _config;
        private IIdentityServerConfiguration _identityServerConfiguration;

        public HttpPeer(string hostAndPort, HttpClient httpClient, IWebHostBuilder builder, IOcelotConfiguration config, IIdentityServerConfiguration identityServerConfiguration)
        {
            _identityServerConfiguration = identityServerConfiguration;
            _config = config;
            Id  = hostAndPort;
            _hostAndPort = hostAndPort;
            _httpClient = httpClient;
            _jsonSerializerSettings = new JsonSerializerSettings() { 
                TypeNameHandling = TypeNameHandling.All
            };
            _baseSchemeUrlAndPort = builder.GetSetting(WebHostDefaults.ServerUrlsKey);
        }

        public string Id {get; private set;}

        public RequestVoteResponse Request(RequestVote requestVote)
        {
            if(_token == null)
            {
                SetToken();
            }

            var json = JsonConvert.SerializeObject(requestVote, _jsonSerializerSettings);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = _httpClient.PostAsync($"{_hostAndPort}/administration/raft/requestvote", content).GetAwaiter().GetResult();
            if(response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<RequestVoteResponse>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), _jsonSerializerSettings);
            }
            else
            {
                return new RequestVoteResponse(false, requestVote.Term);
            }
        }

        public AppendEntriesResponse Request(AppendEntries appendEntries)
        {
            try
            {
                if(_token == null)
                {
                    SetToken();
                }                
                var json = JsonConvert.SerializeObject(appendEntries, _jsonSerializerSettings);
                var content = new StringContent(json);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = _httpClient.PostAsync($"{_hostAndPort}/administration/raft/appendEntries", content).GetAwaiter().GetResult();
                if(response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<AppendEntriesResponse>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),_jsonSerializerSettings);
                }
                else
                {
                    return new AppendEntriesResponse(appendEntries.Term, false);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return new AppendEntriesResponse(appendEntries.Term, false);
            }
        }

        public Response<T> Request<T>(T command) where T : ICommand
        {
            if(_token == null)
            {
                SetToken();
            }   
            var json = JsonConvert.SerializeObject(command, _jsonSerializerSettings);
            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = _httpClient.PostAsync($"{_hostAndPort}/administration/raft/command", content).GetAwaiter().GetResult();
            if(response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<OkResponse<T>>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), _jsonSerializerSettings);
            }
            else 
            {
                return new ErrorResponse<T>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), command);
            }
        }

        private void SetToken()
        {
            var tokenUrl = $"{_baseSchemeUrlAndPort}{_config.AdministrationPath}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _identityServerConfiguration.ApiName),
                new KeyValuePair<string, string>("client_secret", _identityServerConfiguration.ApiSecret),
                new KeyValuePair<string, string>("scope", _identityServerConfiguration.ApiName),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            };
            var content = new FormUrlEncodedContent(formData);
            var response = _httpClient.PostAsync(tokenUrl, content).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_token.TokenType, _token.AccessToken);
        }
    }
}

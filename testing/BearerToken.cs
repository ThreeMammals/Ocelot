using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Ocelot.Testing;

public class BearerToken
{
    [JsonInclude]
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }

    [JsonInclude]
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonInclude]
    [JsonProperty("token_type")]
    public string? TokenType { get; set; }
}

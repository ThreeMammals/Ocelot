using Microsoft.IdentityModel.Tokens;
using Ocelot.Infrastructure.Extensions;
using System.Text;
using System.Text.Json.Serialization;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationTokenRequest
{
    [JsonInclude]
    public string Audience { get; set; }

    [JsonInclude]
    public string UserId { get; set; }

    [JsonInclude]
    public string UserName { get; set; }

    [JsonInclude]
    public string ApiSecret
    {
        get => _apiSecret;
        set
        {
            _apiSecret = value;
            _issuerSigningKey = null;
        }
    }

    [JsonInclude]
    public string Scope { get; set; }

    [JsonInclude]
    public List<KeyValuePair<string, string>> Claims { get; set; } = new();

    private SymmetricSecurityKey _issuerSigningKey;
    private string _apiSecret;

    public SymmetricSecurityKey IssuerSigningKey()
    {
        if (_issuerSigningKey != null)
            return _issuerSigningKey;
        if (_apiSecret.IsEmpty())
            return _issuerSigningKey = null;

        // System.ArgumentOutOfRangeException: 'IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'HS256', the key size must be greater than: '256' bits, key has '160' bits. (Parameter 'keyBytes')'
        // Make sure the security key is at least 32 characters long,
        // So, multiply the password body by repeating it.
        int size = 256 / 8,
            length = _apiSecret.Length;
        var securityKey = length >= size ? _apiSecret
            : string.Join('|', Enumerable.Repeat(_apiSecret, size / length))
                + _apiSecret[..(size % length)]; // total length should be 32 chars
        return _issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
    }
}

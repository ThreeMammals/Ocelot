using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Ocelot.Infrastructure;

public static class JsonSerializerOptionsFactory
{
    public static readonly JsonSerializerOptions Web = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public static readonly JsonSerializerOptions WebWriteIndented = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true
    };
}

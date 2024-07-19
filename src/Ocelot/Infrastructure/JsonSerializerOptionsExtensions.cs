using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Ocelot.Infrastructure
{
    public static class JsonSerializerOptionsExtensions
    {
        public static readonly JsonSerializerOptions Web = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),

        };

        public static readonly JsonSerializerOptions WebWriteIndented = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
    }
}

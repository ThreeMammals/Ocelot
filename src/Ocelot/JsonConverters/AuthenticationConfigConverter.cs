using System;
using Newtonsoft.Json;
using Ocelot.Configuration;

namespace Ocelot.AcceptanceTests
{
    using Newtonsoft.Json.Linq;
    public class AuthenticationConfigConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var setting = default(IAuthenticationConfig);

            if (jsonObject["Provider"] != null)
            {
                switch (jsonObject["Provider"].Value<string>())
                {
                    case "Jwt":
                        setting = new JwtConfig(
                            jsonObject["Authority"].Value<string>(),
                            jsonObject["Audience"].Value<string>());
                        break;

                    default:
                        setting = new IdentityServerConfig(
                            jsonObject["ProviderRootUrl"].Value<string>(),
                            jsonObject["ApiName"].Value<string>(),
                            jsonObject["RequireHttps"].Value<bool>(),
                            jsonObject["ApiSecret"].Value<string>());
                        break;
                }
            }
            else
            {
                setting = new IdentityServerConfig(string.Empty, string.Empty, false, string.Empty);
            }

            serializer.Populate(jsonObject.CreateReader(), setting);
            return setting;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IAuthenticationConfig);
        }
    }

   
}

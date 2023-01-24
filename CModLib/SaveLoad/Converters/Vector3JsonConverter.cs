using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CModLib.SaveLoad.Converters
{
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObj = JObject.Load(reader);

            existingValue.x = jObj.Value<float>("x");
            existingValue.y = jObj.Value<float>("y");
            existingValue.z = jObj.Value<float>("z");

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }
    }
}

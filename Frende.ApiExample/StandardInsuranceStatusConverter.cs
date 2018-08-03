using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frende.ApiExample
{
    public class StandardInsuranceStatusConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IStandardInsuranceStatus))
            {
                
                var jsonStandardInsuranceStatusObject = (JObject)JObject.Load(reader);

                var standardInsuranceStatusType = jsonStandardInsuranceStatusObject.Properties().First().Name;
                var standardInsuranceStatusContent = jsonStandardInsuranceStatusObject.First.First;

                switch (standardInsuranceStatusType)
                {
                    case "active":
                        return serializer.Deserialize<StandardActive>(standardInsuranceStatusContent.CreateReader());
                    case "future":
                        return serializer.Deserialize<StandardFuture>(standardInsuranceStatusContent.CreateReader());
                    default:
                        return serializer.Deserialize<StandardUnknown>(standardInsuranceStatusContent.CreateReader());
                }
                
            }

            throw new JsonException("Expected object to be of type IStandardInsuranceStatus.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IEnumerable<IStandardInsuranceStatus>);
        }
    }
}

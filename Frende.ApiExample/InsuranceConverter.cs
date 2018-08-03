using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frende.ApiExample
{
    public class InsuranceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IEnumerable<IInsurance>))
            {
                var results = new List<IInsurance>();

                var jsonInsuranceTokens = JArray.Load(reader);

                foreach (var jsonInsuranceToken in jsonInsuranceTokens)
                {
                    var jsonInsuranceObject = (JObject)jsonInsuranceToken;

                    var insuranceType = jsonInsuranceObject.Properties().First().Name;
                    var insuranceContent = jsonInsuranceObject.First.First;

                    switch (insuranceType)
                    {
                        case "pet":
                            results.Add(serializer.Deserialize<PetInsurance>(insuranceContent.CreateReader()));
                            break;
                        case "car":
                            results.Add(serializer.Deserialize<CarInsurance>(insuranceContent.CreateReader()));
                            break;
                        default:
                            results.Add(serializer.Deserialize<UnimplementedInsurance>(insuranceContent.CreateReader()));
                            break;
                    }
                }

                return results;
            }

            throw new JsonException("Expected object to be of type IEnumerable<IInsurance>.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IEnumerable<IInsurance>);
        }
    }
}

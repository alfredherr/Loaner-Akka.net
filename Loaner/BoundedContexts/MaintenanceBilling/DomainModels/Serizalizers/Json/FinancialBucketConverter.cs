using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels.Serizalizers.Json
{
    public class FinancialBucketConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IFinancialBucket);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var funancialBucket = default(IFinancialBucket);
            //Console.WriteLine($"FinancialBucketConverter: will attempt to deserialize {jsonObject.ToString()}.");

            var bucketAmount = jsonObject["Amount"].Value<double>();

            switch (jsonObject["Name"].Value<string>())
            {
                case "Dues":
                    funancialBucket = new Dues(bucketAmount);
                    break;
                case "Tax":
                    funancialBucket = new Tax(bucketAmount);
                    break;
                case "Reserve":
                    funancialBucket = new Reserve(bucketAmount);
                    break;
                case "Interest":
                    funancialBucket = new Interest(bucketAmount);
                    break;
                case "Adjustment":
                    funancialBucket = new Adjustment(bucketAmount);
                    break;
            }

            serializer.Populate(jsonObject.CreateReader(), funancialBucket);
            return funancialBucket;
        }
    }
}
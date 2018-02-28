using System;
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
using Nancy.Json;

namespace Loaner.Configuration
{
    public class CustomFinancialBucketConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { yield return typeof(IFinancialBucket); }
        }


        public override object Deserialize(IDictionary<string, object> json, Type type, JavaScriptSerializer serializer)
        {
            if (type == typeof(IFinancialBucket))
            {
                json.TryGetValue("name", out var name);
                json.TryGetValue("value", out var bucketAmount);

                if (name is string && bucketAmount is double)
                    switch (name)
                    {
                        case "Dues":
                            return new Dues((double) bucketAmount);

                        case "Tax":
                            return new Tax((double) bucketAmount);

                        case "Reserve":
                            return new Reserve((double) bucketAmount);

                        case "Interest":
                            return new Interest((double) bucketAmount);

                        default:
                            return null;
                    }
            }

            return null;
        }

        public override IDictionary<string, object> Serialize(object jsonObject, JavaScriptSerializer serializer)
        {
            switch (jsonObject)
            {
                case IFinancialBucket bucket:
                    //Console.WriteLine(
                    //    $"FinancialBucketConverter: will attempt to Serialize {bucket.GetType().Name}.");
                    var json = new Dictionary<string, object>
                    {
                        ["name"] = bucket.Name,
                        ["amonut"] = bucket.Amount
                    };
                    return json;

                default:
                    return null;
            }
        }
    }
}
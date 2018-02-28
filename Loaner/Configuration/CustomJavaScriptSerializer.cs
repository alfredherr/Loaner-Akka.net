using Nancy.Json;

namespace Loaner.Configuration
{
    public class CustomJavaScriptSerializer : JavaScriptSerializer
    {
        public CustomJavaScriptSerializer()
        {
            RegisterConverters(new[] {new CustomFinancialBucketConverter()});
        }
    }
}
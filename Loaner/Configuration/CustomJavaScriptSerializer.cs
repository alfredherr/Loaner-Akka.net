using Nancy.Json;

namespace Loaner.Configuration
{
    public class CustomJavaScriptSerializer : JavaScriptSerializer
    {
        public CustomJavaScriptSerializer()
        {
            this.RegisterConverters(new[]{new CustomFinancialBucketConverter()});
        }
    }
}
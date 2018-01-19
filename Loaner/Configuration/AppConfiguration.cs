using Loaner.Configuration.Models;

namespace Loaner.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public NancyLogging Logging { get; set; }
        public Smtp Smtp { get; set; }
    }
}
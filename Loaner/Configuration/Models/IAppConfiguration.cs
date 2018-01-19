namespace Loaner.Configuration.Models
{
    public interface IAppConfiguration
    {
        NancyLogging Logging { get; }
        Smtp Smtp { get; }
    }
}
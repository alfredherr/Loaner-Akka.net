namespace Loaner.Configuration
{

    public interface IAppConfiguration
    {
        NancyLogging Logging { get; }
        Smtp Smtp { get; }
    }

}
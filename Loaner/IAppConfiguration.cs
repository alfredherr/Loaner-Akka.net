namespace Loaner
{

    public interface IAppConfiguration
    {
        NancyLogging Logging { get; }
        Smtp Smtp { get; }
    }

}
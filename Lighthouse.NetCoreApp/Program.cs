namespace Lighthouse.NetCoreApp
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using System.Threading.Tasks;
    using System.IO;
    using static ActorManagement.Management;
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"*****************************");
            Console.WriteLine($"****      Lighthouse     ****");
            Console.WriteLine($"*****************************");

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://*:8181")
                .Build();
            host.Run();

        }
    }
}
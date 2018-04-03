using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Lighthouse.NetCoreApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine($"*****************************");
            Console.WriteLine($"****      Lighthouse     ****");
            Console.WriteLine($"*****************************");

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseNLog()
                .UseStartup<Startup>()
                .UseUrls("http://*:8181")
                .Build();
            host.Run();
        }
    }
}
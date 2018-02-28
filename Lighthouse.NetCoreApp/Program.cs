using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

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
                .UseStartup<Startup>()
                .UseUrls("http://*:8181")
                .Build();
            host.Run();
        }
    }
}
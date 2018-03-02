using System;
using System.Globalization;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Loaner
{
    public class Program
    {
        private static void Main()
        {
            IPAddress ip = IPAddress.Any;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            
            Console.WriteLine($"*****************************");
            Console.WriteLine($"**** Akka & DotNet Core  ****");
            Console.WriteLine($"********{ip}************");

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel(options =>
                {
                    //options.Limits.MaxConcurrentConnections = 100;
                    //options.Limits.MaxConcurrentUpgradedConnections = 100;
                    //options.Limits.MaxRequestBodySize = 10 * 1024;
                    //options.Limits.MinRequestBodyDataRate =
                    //    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    //options.Limits.MinResponseDataRate =
                    //    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    options.Listen(ip, 8080);
                    options.Listen(ip, 8443, listenOptions => { listenOptions.UseHttps("mycert.pfx", "Testing"); });
                })
                .UseStartup<Startup>()
                .Build();
            host.Run();
        }
    }
}

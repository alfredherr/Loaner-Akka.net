
namespace Loaner
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using System.IO;
    using System.Net;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    

    public class Program
    {
        static void Main()
        {
            var ip = IPAddress.Loopback;

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
                    options.Listen(ip, 8443, listenOptions =>
                    {
                        listenOptions.UseHttps("localhost.pfx", "Testing");
                    });

                })
                .UseStartup<Startup>()
	            .Build();
            host.Run();
        }
    }
}

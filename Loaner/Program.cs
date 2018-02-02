namespace Loaner
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using System.IO;

    public class Program
    {
        static void Main()
        {
            Console.WriteLine($"*****************************");
            Console.WriteLine($"**** Akka & DotNet Core  ****");
            Console.WriteLine($"*****************************");

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
	        .UseUrls("http://*:8080")
                .Build();
            host.Run();
        }
    }
}

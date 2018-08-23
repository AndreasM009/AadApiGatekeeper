using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace K8SSideCar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var port = Environment.GetEnvironmentVariable("Api__Port");

            var builder = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

            if (null != port)
            {
                builder.UseUrls($"http://0.0.0.0:{port}");
            }

            return builder.Build();
        }
    }
}

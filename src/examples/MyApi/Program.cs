using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace MyApi
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

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace L1.IdentityServerApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add configuration
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Add Sentry
            builder.WebHost.UseSentry(opt =>
            {
                opt.Environment = builder.Configuration["Env"];
                opt.SendDefaultPii = true;
                opt.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
            });

            // Configure port
            builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["AppSettings:HostingPort"]}");

            // Add services from Startup.cs
            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();

            // Configure middleware from Startup.cs
            startup.Configure(app, app.Environment);

            app.Run();
        }
    }
}
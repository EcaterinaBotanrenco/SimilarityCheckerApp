using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimilarityChecker.UI.Authentication;
using SimilarityChecker.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimilarityChecker.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);



            builder.Services.AddHttpClient("Api", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7260");
            });

            CreateHostBuilder(args).Build().Run();

            builder.Services.AddScoped<IDocumentScanApiClient>(sp =>
            {
                var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api");
                var sessionStore = sp.GetRequiredService<AuthSessionStore>();

                return new DocumentScanApiClient(http, sessionStore);
            });
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

    }
}

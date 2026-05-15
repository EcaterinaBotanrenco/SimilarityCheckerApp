using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimilarityChecker.UI.Authentication;
using SimilarityChecker.UI.Services;
using System;

namespace SimilarityChecker.UI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllers();

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            services.AddScoped<AuthSessionStore>();

            services.AddScoped<CustomAuthStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<CustomAuthStateProvider>());
            services.AddServerSideBlazor()
                .AddHubOptions(options =>
                {
                    options.MaximumReceiveMessageSize = 50 * 1024 * 1024; // 50 MB
                });

            var apiBase = Configuration["ApiBaseUrl"] ?? "https://localhost:7260/";

            services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
            });

            services.AddHttpClient<IDocumentScanApiClient, DocumentScanApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
            });

            services.AddHttpClient<IPlagiarismApiClient, PlagiarismApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
            });

            services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBase);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

        }
    }
}

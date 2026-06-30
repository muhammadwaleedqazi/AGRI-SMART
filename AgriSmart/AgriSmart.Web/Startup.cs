using System;
using AgriSmart.Web.Data;
using AgriSmart.Web.Models;
using AgriSmart.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgriSmart.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public static bool UseSqlServer { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var provider = Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
            UseSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);

            var connectionString = UseSqlServer
                ? (Configuration.GetConnectionString("SqlServer") ?? Configuration.GetConnectionString("DefaultConnection"))
                : (Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=agrismart.db");

            services.AddDbContext<AppDbContext>(options =>
            {
                if (UseSqlServer)
                    options.UseSqlServer(connectionString);
                else
                    options.UseSqlite(connectionString);
            });

            services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(
                    Configuration.GetValue<int>("Session:InactivityMinutes", 60));
                options.SlidingExpiration = true;
            });

            services.AddScoped<IdentityAuthenticationStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<IdentityAuthenticationStateProvider>());

            services.AddScoped<CropRecommendationService>();
            services.AddHttpClient();
            services.AddScoped<WeatherService>();
            services.AddScoped<PestDiseaseService>();
            services.AddScoped<AdvisoryService>();
            services.AddScoped<AppState>();
            services.AddMemoryCache();
            services.AddScoped<EmailService>();
            services.AddScoped<GeminiChatService>();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddAuthorization();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Error");

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}

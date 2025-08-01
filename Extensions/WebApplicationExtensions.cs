using AonFreelancing.Enums;
using AonFreelancing.Hubs;
using AonFreelancing.Middlewares;
using AonFreelancing.Models;
using AonFreelancing.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using System.Net;

namespace AonFreelancing.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder) 
        {
            var kestrelConfig = builder.Configuration.GetSection("Kestrel:Endpoints");

            string httpUrl = kestrelConfig["Http:Url"] ??
                throw new InvalidOperationException("Http URL configuration is missing");
            string httpsUrl = kestrelConfig["Https:Url"] ??
                throw new InvalidOperationException("Https URL configuration is missing");

            bool isSslEnabled = kestrelConfig.GetValue<bool>("Https:Ssl:Enabled");

            if (isSslEnabled)
            {
                ConfigureKestrelWithSsl(builder, httpUrl, httpsUrl, kestrelConfig);
            }
            else
            {
                builder.WebHost.UseUrls(httpUrl, httpsUrl);
            }

            return builder;
        }

        private static void ConfigureKestrelWithSsl(WebApplicationBuilder builder, string httpUrl, string httpsUrl, IConfigurationSection kestrelConfig)
        {
            string certPath = kestrelConfig["Https:Ssl:CertPath"] ??
                throw new InvalidOperationException("SSL certificate path is missing");
            string certPassword = kestrelConfig["Https:Ssl:Password"] ??
                throw new InvalidOperationException("SSL certificate password is missing");

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, new Uri(httpUrl).Port);
                options.Listen(IPAddress.Any, new Uri(httpsUrl).Port, listenOptions =>
                {
                    listenOptions.UseHttps(certPath, certPassword);
                });
                options.Configure(config: builder.Configuration);
            });
        }
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseHttpsRedirection();

            app.ConfigureStaticFiles();

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationsHub>("/Hubs/Notifications");
            app.MapHub<WebRtcSignalingHub>("/Hubs/WebRtcSignaling");

            return app;
        }
        private static void ConfigureStaticFiles(this WebApplication app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(FileStorageService.ROOT),
                RequestPath = "/images"
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/html")),
                RequestPath = "/pages"
            });
        }

        public static void SeedRoles(this WebApplication app)
        {
            using var serviceScope = app.Services.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            SeedRolesAsync(roleManager).GetAwaiter().GetResult();
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            foreach (string roleName in Enum.GetNames(typeof(UserRoles)))
            {
                string normalizedRoleName = roleName.ToLower();
                if (!await roleManager.RoleExistsAsync(normalizedRoleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = normalizedRoleName });
                }
            }
        }
    }
}

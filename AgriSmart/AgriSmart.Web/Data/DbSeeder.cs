using System;
using System.Linq;
using System.Threading.Tasks;
using AgriSmart.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgriSmart.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            if (Startup.UseSqlServer)
            {
                try { await context.Database.MigrateAsync(); }
                catch { await context.Database.EnsureCreatedAsync(); }
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            foreach (var role in new[] { "Admin", "Farmer" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
            }

            var admin = await userManager.FindByNameAsync("admin");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@agrismart.pk",
                    FullName = "System Administrator",
                    Region = "Islamabad",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@1234");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                admin.IsActive = true;
                admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, "Admin@1234");
                var result = await userManager.UpdateAsync(admin);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to update seeded admin user: {Errors}", string.Join(", ", result.Errors));
                }
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Seed default farmer account ──────────────────────────────────────
            var farmer = await userManager.FindByNameAsync("farmer");
            if (farmer == null)
            {
                farmer = new ApplicationUser
                {
                    UserName = "farmer",
                    Email = "farmer@agrismart.pk",
                    FullName = "Demo Farmer",
                    Region = "Punjab",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                var farmerResult = await userManager.CreateAsync(farmer, "Farmer@1234");
                if (farmerResult.Succeeded)
                    await userManager.AddToRoleAsync(farmer, "Farmer");
            }
            else
            {
                farmer.IsActive = true;
                farmer.PasswordHash = userManager.PasswordHasher.HashPassword(farmer, "Farmer@1234");
                await userManager.UpdateAsync(farmer);
                if (!await userManager.IsInRoleAsync(farmer, "Farmer"))
                    await userManager.AddToRoleAsync(farmer, "Farmer");
            }

            // ── Seed advisory records (only if table is empty) ───────────────────
            if (!await context.Advisories.AnyAsync())
            {
                var advisories = new[]
                {
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 20), CropName = "Rice", Description = "Apply nitrogen fertilizer in the morning.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 18), CropName = "Cotton", Description = "Monitor for whitefly and spray neem oil.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 15), CropName = "Maize", Description = "Irrigation recommended after 7 days.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 12), CropName = "Wheat", Description = "Harvest crop when grains are hard.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 10), CropName = "Sugarcane", Description = "Apply potash fertilizer at tillering stage.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 8), CropName = "Rice", Description = "Inspect fields for stem borer moths.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 5), CropName = "Cotton", Description = "Schedule drip irrigation every 5 days.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 5, 3), CropName = "Soybean", Description = "Harvest pods when they turn yellow-brown.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 28), CropName = "Maize", Description = "Apply DAP fertilizer at sowing time.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 25), CropName = "Wheat", Description = "Watch for rust disease on leaves.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 20), CropName = "Rice", Description = "Ensure standing water depth of 5 cm.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 15), CropName = "Sugarcane", Description = "Cut mature cane at ground level.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 10), CropName = "Wheat", Description = "Top-dress urea fertilizer before final irrigation.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 8), CropName = "Potato", Description = "Spray fungicide to prevent late blight disease.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 5), CropName = "Cotton", Description = "First irrigation 30-35 days after sowing.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 4, 2), CropName = "Maize", Description = "Harvest maize when husks turn paper-like and grains are dry.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 28), CropName = "Sugarcane", Description = "Apply nitrogenous fertilizer in three split doses.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 25), CropName = "Tomato", Description = "Use yellow sticky cards to capture whiteflies.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 22), CropName = "Rice", Description = "Maintain water level during tillering stage.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 18), CropName = "Wheat", Description = "Harvest when grain moisture content drops below 14%.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 15), CropName = "Citrus", Description = "Apply compost and zinc sulfate around tree basins.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 12), CropName = "Mango", Description = "Spray against hopper insects during flowering stage.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 9), CropName = "Chilli", Description = "Irrigate lightly at flowering to prevent flower drop.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 5), CropName = "Onion", Description = "Harvest onions when 50% of tops fall over.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 3, 2), CropName = "Sunflower", Description = "Apply nitrogen-phosphorus compound fertilizer.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 28), CropName = "Cotton", Description = "Treat seeds with suitable pesticide before sowing.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 25), CropName = "Tomato", Description = "Drip irrigation recommended to avoid root rot.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 22), CropName = "Wheat", Description = "Apply second dose of urea at jointing stage.", Tag = "Fertilizer" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 18), CropName = "Rice", Description = "Harvest when 80-85% of grains turn straw-colored.", Tag = "Harvesting" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 15), CropName = "Maize", Description = "Inspect leaves for fall armyworm larvae.", Tag = "Pest Control" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 12), CropName = "Sugarcane", Description = "Irrigate crop at 10-12 days interval in winter.", Tag = "Irrigation" },
                    new AdvisoryRecord { Date = new DateTime(2025, 2, 8), CropName = "Potato", Description = "Dehaulm potato crop 10 days before harvesting.", Tag = "Harvesting" },
                };

                await context.Advisories.AddRangeAsync(advisories);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} advisory records.", advisories.Length);
            }

            logger.LogInformation("Database seeding completed ({Provider}).",
                Startup.UseSqlServer ? "SQL Server" : "SQLite");
        }
    }
}

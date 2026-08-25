using AtcSimulationFramework.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AtcDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.AppUsers.AnyAsync())
        {
            var defaultUsers = new List<AppUser>
            {
                new()
                {
                    Username = "controller1",
                    Email = "controller1@atc-sim.local",
                    Role = UserRole.Controller,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Username = "supervisor",
                    Email = "supervisor@atc-sim.local",
                    Role = UserRole.Supervisor,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Username = "admin",
                    Email = "admin@atc-sim.local",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.AppUsers.AddRangeAsync(defaultUsers);
            await context.SaveChangesAsync();
        }
    }
}

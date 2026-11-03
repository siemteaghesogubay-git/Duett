using Duett.API.Models;
using Microsoft.AspNetCore.Identity;

namespace Duett.API.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();

        string[] roles = ["USER", "ADMIN"];

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Name = roleName
                });

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Kunde inte skapa rollen {roleName}: {errors}");
            }
        }
    }
}
using Microsoft.AspNetCore.Identity;
using PatitasAPI.Core.Entities;

namespace PatitasAPI.Infraestructure.Seeders;

public static class AuthSeeder
{   
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Dev", "ShelterOwner", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedDefaultUserAsync(UserManager<AppUser> userManager)
    {
        var defaultPassword = PatitasEnv.GetEnvVariable("AUTH_DEFAULT_PASSWORD");

        // Admin
        var adminEmail = "admin@patitas.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {            var newAdmin = new AppUser
            {
                UserName = adminEmail, 
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Principal",
                Gender = Gender.MALE,
                BirthDate = new DateOnly(2005, 1, 1)
            };

            var result = await userManager.CreateAsync(newAdmin, defaultPassword);
            if (result.Succeeded) await userManager.AddToRoleAsync(newAdmin, "Dev");
        }

        // VOluntario
        var voluntarioEmail = "voluntario@patitas.com";
        if (await userManager.FindByEmailAsync(voluntarioEmail) == null)
        {
            var newVoluntario = new AppUser
            {
                UserName = voluntarioEmail, 
                Email = voluntarioEmail,
                EmailConfirmed = true,
                FirstName = "Juan",
                LastName = "Voluntario",
                Gender = Gender.MALE,
                BirthDate = new DateOnly(2007, 6, 8)
            };

            var result = await userManager.CreateAsync(newVoluntario, defaultPassword);
            if (result.Succeeded) await userManager.AddToRoleAsync(newVoluntario, "ShelterOwner");
        }

        // Adoptante
        var adoptanteEmail = "adoptante@patitas.com";
        if (await userManager.FindByEmailAsync(adoptanteEmail) == null)
        {
            var newAdoptante = new AppUser
            {
                UserName = adoptanteEmail, 
                Email = adoptanteEmail,
                EmailConfirmed = true,
                FirstName = "Maria",
                LastName = "Adoptante",
                Gender = Gender.FEMALE,
                BirthDate = new DateOnly(2005, 1, 1)
            };

            var result = await userManager.CreateAsync(newAdoptante, defaultPassword);
            if (result.Succeeded) await userManager.AddToRoleAsync(newAdoptante, "User");
        }
    }
}
// Services/RoleSeederService.cs
using Microsoft.AspNetCore.Identity;

public class RoleSeederService(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration config)
{
    public static readonly string[] Roles = ["Admin", "Engineer", "Viewer"];

    public async Task SeedAsync()
    {
        // 1. Create roles if they don't exist
        foreach (var role in Roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // 2. Create default admin from appsettings (never hardcode credentials)
        var adminEmail = config["AdminSeed:Email"];
        var adminPass = config["AdminSeed:Password"];

        if (adminEmail is null || adminPass is null) return;

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Department = "IT",
                EmailConfirmed = true   // skip email confirmation for seed
            };

            var result = await userManager.CreateAsync(admin, adminPass);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
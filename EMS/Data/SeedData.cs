using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.Data
{
    public static class SeedData
    {
        private static readonly string[] Roles = ["Admin", "Teacher", "Student", "Staff"];

        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create role '{role}': {string.Join(", ", result.Errors)}");
                    }
                }
            }

            var adminEmail = configuration["AdminUser:Email"];
            var adminUserName = configuration["AdminUser:UserName"] ?? adminEmail;
            var adminPassword = configuration["AdminUser:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new Exception("AdminUser:Email and AdminUser:Password must be set in configuration (use user secrets or env vars).");
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors)}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
                if (!addToRoleResult.Succeeded)
                {
                    throw new Exception($"Failed to add admin user to role: {string.Join(", ", addToRoleResult.Errors)}");
                }
            }

            await userManager.SetLockoutEnabledAsync(admin, false);
        }
    }
}
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Crispy.Infrastructure.Data
{
    public static class RolesInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            string[] roleNames = { "Admin", "User" };

            // Створення ролей
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // Перевірка чи існує хоча б один адмін
            var adminUser = await userManager.FindByEmailAsync("admin@crispy.com");
            if (adminUser == null)
            {
                var powerUser = new User
                {
                    UserName = "Admin",
                    Email = "admin@crispy.com",
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow
                };

                string userPWD = "AdminPassword123!"; // Змініть пароль в продакшені
                var createPowerUser = await userManager.CreateAsync(powerUser, userPWD);
                if (createPowerUser.Succeeded)
                {
                    // Призначення ролі Admin створеному користувачу
                    await userManager.AddToRoleAsync(powerUser, "Admin");
                }
            }
        }
    }
}
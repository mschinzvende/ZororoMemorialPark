using Microsoft.AspNetCore.Identity;

namespace ZororoMemorialPark.Data
{
    public static class RoleMigrationSeeder
    {
        private static readonly string[] NewRoles = new[]
        {
            "Manager",
            "OperationsOfficer",
            "FinanceOfficer",
            "Agent"
            
        };

        // Old → New role map
        private static readonly Dictionary<string, string> RoleMap = new()
        {
            { "Admin", "Manager" },
            { "Accountant", "FinanceOfficer" },
            { "Supervisor", "OperationsOfficer" },
            { "Agent", "Agent" },

            
        };

        public static async Task MigrateRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Create new roles if missing
            foreach (var role in NewRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Migrate users to new roles
            foreach (var map in RoleMap)
            {
                string oldRole = map.Key;
                string newRole = map.Value;

                if (await roleManager.RoleExistsAsync(oldRole))
                {
                    var usersInOldRole = await userManager.GetUsersInRoleAsync(oldRole);

                    foreach (var user in usersInOldRole)
                    {
                        if (!await userManager.IsInRoleAsync(user, newRole))
                        {
                            await userManager.AddToRoleAsync(user, newRole);
                        }
                    }
                }
            }

            // 3. OPTIONAL: delete old roles after migration
            foreach (var oldRole in RoleMap.Keys)
            {
                var role = await roleManager.FindByNameAsync(oldRole);
                if (role != null && role.Name != "Agent")
                    await roleManager.DeleteAsync(role); // Comment out if you want to keep them
            }

            // 4. Ensure a Manager (Admin) user exists
            string adminEmail = "admin@zororo.co.zw";
            string adminPassword = "ZororoAdmin@2025";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(adminUser, adminPassword);

                if (created.Succeeded)
                    await userManager.AddToRoleAsync(adminUser, "Manager");
            }
        }
    }
}

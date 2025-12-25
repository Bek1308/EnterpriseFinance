using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFinance.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Birinchi Tenant yaratish
            if (!await dbContext.Tenants.AnyAsync())
            {
                var mainTenant = new Tenant
                {
                    Name = "Main Company",
                    Currency = "UZS",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                dbContext.Tenants.Add(mainTenant);
                await dbContext.SaveChangesAsync();
            }

            var tenant = await dbContext.Tenants.FirstAsync();

            // 2. Admin roli yaratish
            const string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // 3. Super Admin foydalanuvchi yaratish
            const string adminEmail = "admin@company.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    TenantId = tenant.Id
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123");

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                    Console.WriteLine("Super Admin created: admin@company.com / Admin@123");
                }
                else
                {
                    Console.WriteLine("Admin creation failed: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // 4. Qo‘shimcha rollar
            string[] otherRoles = { "Accountant", "Manager", "User" };
            foreach (var role in otherRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 5. Dastlabki kategoriyalar
            if (!await dbContext.Categories.AnyAsync())
            {
                var defaultCategories = new List<Category>
                {
                    // Income
                    new Category { TenantId = tenant.Id, Name = "Sales Revenue", IsIncome = true, Color = "#4CAF50", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Service Income", IsIncome = true, Color = "#8BC34A", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Other Income", IsIncome = true, Color = "#CDDC39", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },

                    // Expense
                    new Category { TenantId = tenant.Id, Name = "Salary", IsIncome = false, Color = "#F44336", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Rent", IsIncome = false, Color = "#E91E63", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Utilities", IsIncome = false, Color = "#FF9800", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Office Supplies", IsIncome = false, Color = "#FF5722", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Marketing", IsIncome = false, Color = "#9C27B0", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
                    new Category { TenantId = tenant.Id, Name = "Other Expenses", IsIncome = false, Color = "#607D8B", CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
                };

                dbContext.Categories.AddRange(defaultCategories);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
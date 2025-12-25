// ApplicationDbContext.cs (SQLite uchun to'liq ishlaydigan versiya – "max" xatosi butunlay hal qilindi)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EnterpriseFinance.Models;

namespace EnterpriseFinance.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // DbSet lar
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<RecurringTransaction> RecurringTransactions { get; set; }
        public DbSet<PlannedTransaction> PlannedTransactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // === SQLite uchun Identity jadvallaridagi "nvarchar(max)" maydonlarni TEXT ga o'zgartirish ===
            // Bu qatorlar "near "max": syntax error" xatosini 100% yo'qotadi
            builder.Entity<IdentityRole>(entity =>
            {
                entity.Property(r => r.ConcurrencyStamp).HasColumnType("TEXT");
            });

            builder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.ConcurrencyStamp).HasColumnType("TEXT");
                entity.Property(u => u.SecurityStamp).HasColumnType("TEXT");
                entity.Property(u => u.PasswordHash).HasColumnType("TEXT");
                entity.Property(u => u.PhoneNumber).HasColumnType("TEXT");
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(t => t.Value).HasColumnType("TEXT");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(l => l.ProviderDisplayName).HasColumnType("TEXT");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.Property(rc => rc.ClaimValue).HasColumnType("TEXT");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.Property(uc => uc.ClaimValue).HasColumnType("TEXT");
            });

            // === Qolgan konfiguratsiyalar (multi-tenant, soft delete va h.k.) ===
            builder.Entity<AppUser>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            foreach (var fk in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => fk.PrincipalEntityType.Name.Contains("Tenant") &&
                             fk.DeleteBehavior == DeleteBehavior.Cascade))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }

            builder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<RecurringTransaction>()
                .HasOne(r => r.Category)
                .WithMany(c => c.RecurringTransactions)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PlannedTransaction>()
                .HasOne(p => p.Category)
                .WithMany(c => c.PlannedTransactions)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Budget>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Budgets)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Transaction>()
                .HasOne(t => t.PlannedTransaction)
                .WithOne(p => p.ExecutedTransaction)
                .HasForeignKey<Transaction>(t => t.PlannedTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PlannedTransaction>()
                .HasOne(p => p.RecurringTransaction)
                .WithMany(r => r.GeneratedPlannedTransactions)
                .HasForeignKey(p => p.RecurringTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            var currentTenantId = GetCurrentTenantId();

            builder.Entity<Tenant>()
                .HasQueryFilter(t => !t.IsDeleted);

            builder.Entity<Category>()
                .HasQueryFilter(c => c.TenantId == currentTenantId && !c.IsDeleted);

            builder.Entity<Transaction>()
                .HasQueryFilter(t => t.TenantId == currentTenantId && !t.IsDeleted);

            builder.Entity<RecurringTransaction>()
                .HasQueryFilter(r => r.TenantId == currentTenantId && !r.IsDeleted);

            builder.Entity<PlannedTransaction>()
                .HasQueryFilter(p => p.TenantId == currentTenantId);

            builder.Entity<Budget>()
                .HasQueryFilter(b => b.TenantId == currentTenantId && !b.IsDeleted);

            builder.Entity<Transaction>()
                .HasIndex(t => new { t.TenantId, t.Date });

            builder.Entity<PlannedTransaction>()
                .HasIndex(p => new { p.TenantId, p.PlannedDate, p.Status });

            builder.Entity<Budget>()
                .HasIndex(b => new { b.TenantId, b.Year, b.Month });

            // Decimal precision
            builder.Entity<Transaction>().Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Entity<RecurringTransaction>().Property(r => r.Amount).HasColumnType("decimal(18,2)");
            builder.Entity<PlannedTransaction>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
            builder.Entity<Budget>().Property(b => b.PlannedIncome).HasColumnType("decimal(18,2)");
            builder.Entity<Budget>().Property(b => b.PlannedExpense).HasColumnType("decimal(18,2)");
        }

        // Audit maydonlari avtomatik to'ldirish
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var currentUserId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Tenant || entry.Entity is Category ||
                    entry.Entity is Transaction || entry.Entity is RecurringTransaction ||
                    entry.Entity is PlannedTransaction || entry.Entity is Budget)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                            entry.Property("CreatedBy").CurrentValue = currentUserId ?? "System";
                            if (entry.Property("IsDeleted").Metadata.PropertyInfo != null)
                            {
                                entry.Property("IsDeleted").CurrentValue = false;
                            }
                            break;

                        case EntityState.Modified:
                            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                            entry.Property("UpdatedBy").CurrentValue = currentUserId ?? "System";
                            break;

                        case EntityState.Deleted:
                            entry.State = EntityState.Modified;
                            entry.Property("IsDeleted").CurrentValue = true;
                            entry.Property("DeletedAt").CurrentValue = DateTime.UtcNow;
                            entry.Property("DeletedBy").CurrentValue = currentUserId ?? "System";
                            break;
                    }
                }
            }
        }

        private int GetCurrentTenantId()
        {
            if (_httpContextAccessor?.HttpContext == null)
                return 1;

            if (_httpContextAccessor.HttpContext.Items["TenantId"] is int tenantId)
            {
                return tenantId;
            }

            return 1;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor?.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
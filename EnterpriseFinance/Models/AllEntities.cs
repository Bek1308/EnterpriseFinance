using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseFinance.Models
{
    // 1. Tenant (Korxona)
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? LogoFileName { get; set; } // Serverdagi fayl nomi, masalan: "logo-123.png"

        [StringLength(10)]
        public string Currency { get; set; } = "UZS";

        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public List<AppUser> Users { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
        public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
        public List<PlannedTransaction> PlannedTransactions { get; set; } = new();
        public List<Budget> Budgets { get; set; } = new();
    }

    // 2. AppUser (IdentityUser dan meros oladi)
    public class AppUser : IdentityUser
    {
        [Required]
        public int TenantId { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
    }

    // 3. Category (Kategoriyalar)
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsIncome { get; set; } // true = Kirim, false = Chiqim

        [StringLength(20)]
        public string? Color { get; set; } = "#6b7280";

        public int? OrderIndex { get; set; } = 0;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public List<Transaction> Transactions { get; set; } = new();
        public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
        public List<PlannedTransaction> PlannedTransactions { get; set; } = new();
        public List<Budget> Budgets { get; set; } = new();
    }

    // 4. Transaction (Haqiqiy amalga oshirilgan kirim/chiqim)
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!; // Qo‘shilgan maydon

        [Required]
        public int TenantId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsIncome { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public int CategoryId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int? PlannedTransactionId { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public PlannedTransaction? PlannedTransaction { get; set; }
    }


    // 5. RecurringTransaction (Doimiy harajat/kirimlar)
    public class RecurringTransaction
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // Qo‘shildi: tranzaksiya nomi (masalan, "Ijara", "Maosh")

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsIncome { get; set; } // Kirim yoki chiqim

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(20)]
        public string Frequency { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Yearly

        public int Interval { get; set; } = 1; // Har necha marta (masalan, har 2 oyda)

        public DayOfWeek? DayOfWeek { get; set; } // Weekly uchun (Monday, Tuesday...)

        public int? DayOfMonth { get; set; } // Monthly uchun (1-31, 31 = oxirgi kun)

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime? EndDate { get; set; } // Null = cheksiz

        public DateTime NextPlannedDate { get; set; } // Keyingi avtomatik yaratiladigan planned tranzaksiya sanasi

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public List<PlannedTransaction> GeneratedPlannedTransactions { get; set; } = new();
    }

    // 6. PlannedTransaction (Rejalashtirilgan operatsiyalar)
    public class PlannedTransaction
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // Yangi qo'shilgan: Tranzaksiya nomi

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsIncome { get; set; }

        [Required]
        public DateTime PlannedDate { get; set; } // Qachon bajarilishi reja qilingan

        public DateTime? ExecutedDate { get; set; } // Haqiqatda qachon bajarilgan

        [Required]
        public int CategoryId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsRecurringGenerated { get; set; } = false;

        public int? RecurringTransactionId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Executed, Cancelled

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false; // Soft delete
        public DateTime? DeletedAt { get; set; } // Soft delete
        public string? DeletedBy { get; set; } // Soft delete

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public RecurringTransaction? RecurringTransaction { get; set; }
        public Transaction? ExecutedTransaction { get; set; }
    }

    // 7. Budget (Oy bo'yicha byudjet)
    public class Budget
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; } // 1-12

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlannedIncome { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlannedExpense { get; set; } = 0;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Category? Category { get; set; }
    }
}
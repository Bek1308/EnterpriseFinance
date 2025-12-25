using EnterpriseFinance.Data;
using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseFinance.Controllers
{
    [Authorize]
    public class RecurringTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecurringTransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentTenantId()
        {
            if (HttpContext.Items["TenantId"] is int tenantId)
            {
                return tenantId;
            }

            throw new InvalidOperationException("Foydalanuvchi hech qanday korxonaga (Tenant) bog‘lanmagan.");
        }

        // GET: RecurringTransactions
        public async Task<IActionResult> Index()
        {
            int tenantId = GetCurrentTenantId();

            var recurring = await _context.RecurringTransactions
                .Where(r => r.TenantId == tenantId && !r.IsDeleted && r.IsActive)
                .Include(r => r.Category)
                .OrderBy(r => r.StartDate)
                .ThenBy(r => r.NextPlannedDate)
                .Select(r => new RecurringTransactionViewModel
                {
                    Id = r.Id,
                    Name = r.Name,                                      
                    Amount = r.Amount,
                    IsIncome = r.IsIncome,
                    Frequency = r.Frequency,
                    Interval = r.Interval,                             
                    DayOfWeek = r.DayOfWeek,                            
                    DayOfMonth = r.DayOfMonth,                          
                    StartDate = r.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = r.EndDate.HasValue
                        ? r.EndDate.Value.ToString("yyyy-MM-dd")
                        : "No end date",
                    NextPlannedDate = r.NextPlannedDate.ToString("yyyy-MM-dd"),
                    CategoryName = r.Category.Name,
                    CategoryColor = r.Category.Color,
                    Description = r.Description,
                    IsActive = r.IsActive                               
                })
                .ToListAsync();

            return View(recurring);
        }

        // GET: RecurringTransactions/Create
        public async Task<IActionResult> Create()
        {
            int tenantId = GetCurrentTenantId();

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name"
            );

            return View(new RecurringTransaction
            {
                StartDate = DateTime.Today,
                NextPlannedDate = DateTime.Today,
                Frequency = "Monthly"
            });
        }

        // POST: RecurringTransactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecurringTransaction recurring)
        {
            int tenantId = GetCurrentTenantId();

            if (recurring.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
            }

            if (recurring.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(recurring.Frequency))
            {
                ModelState.AddModelError("Frequency", "Please select a frequency.");
            }

            recurring.TenantId = tenantId;
            recurring.CreatedAt = DateTime.UtcNow;
            recurring.CreatedBy = User.Identity?.Name ?? "System";
            recurring.NextPlannedDate = recurring.StartDate;

            //if (ModelState.IsValid)
            //{
                try
                {
                    _context.Add(recurring);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the recurring transaction.");
                }
            //}

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                recurring.CategoryId
            );

            return View(recurring);
        }

        // GET: RecurringTransactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetCurrentTenantId();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

            if (recurring == null) return NotFound();

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                recurring.CategoryId
            );

            return View(recurring);
        }

        // POST: RecurringTransactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecurringTransaction recurring)
        {
            if (id != recurring.Id)
            {
                return NotFound();
            }

            int tenantId = GetCurrentTenantId();

            // Qo‘lda validation – ModelState.IsValid ga bog‘liq emas
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(recurring.Name))
            {
                ModelState.AddModelError("Name", "Transaction name is required.");
                hasError = true;
            }

            if (recurring.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
                hasError = true;
            }

            if (recurring.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
                hasError = true;
            }

            if (recurring.Interval < 1)
            {
                ModelState.AddModelError("Interval", "Interval must be at least 1.");
                hasError = true;
            }

            // Agar xato bo‘lsa – ViewBag ni to‘ldirib, viewga qaytamiz
            if (hasError)
            {
                ViewBag.Categories = new SelectList(
                    await _context.Categories
                        .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                        .OrderBy(c => c.OrderIndex)
                        .ThenBy(c => c.Name)
                        .Select(c => new { c.Id, c.Name })
                        .ToListAsync(),
                    "Id",
                    "Name",
                    recurring.CategoryId
                );

                return View(recurring);
            }

            try
            {
                var existing = await _context.RecurringTransactions
                    .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

                if (existing == null)
                {
                    return NotFound();
                }

                // Barcha maydonlarni yangilaymiz (yangi entityga mos)
                existing.Name = recurring.Name;
                existing.Amount = recurring.Amount;
                existing.IsIncome = recurring.IsIncome;
                existing.CategoryId = recurring.CategoryId;
                existing.Frequency = recurring.Frequency;
                existing.Interval = recurring.Interval;
                existing.DayOfWeek = recurring.DayOfWeek;
                existing.DayOfMonth = recurring.DayOfMonth;
                existing.StartDate = recurring.StartDate;
                existing.EndDate = recurring.EndDate;
                existing.Description = recurring.Description;
                existing.IsActive = recurring.IsActive;

                // Audit
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Boshqa foydalanuvchi o‘zgartirgan bo‘lsa
                if (!await _context.RecurringTransactions.AnyAsync(r => r.Id == id && r.TenantId == tenantId))
                {
                    return NotFound();
                }

                ModelState.AddModelError("", "The record was modified by another user. Please reload and try again.");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the recurring transaction. Please try again.");
            }

            // Har qanday exception bo‘lsa – dropdown ni qayta to‘ldiramiz
            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                recurring.CategoryId
            );

            return View(recurring);
        }

        // POST: RecurringTransactions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetCurrentTenantId();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

            if (recurring == null) return NotFound();

            recurring.IsDeleted = true;
            recurring.DeletedAt = DateTime.UtcNow;
            recurring.DeletedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: RecurringTransactions/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            int tenantId = GetCurrentTenantId();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

            if (recurring == null) return NotFound();

            recurring.IsActive = !recurring.IsActive;
            recurring.UpdatedAt = DateTime.UtcNow;
            recurring.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
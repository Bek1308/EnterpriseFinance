// Controllers/BudgetController.cs (to'liq, avvalgi modullar uslubida)
using EnterpriseFinance.Data;
using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseFinance.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetController(ApplicationDbContext context)
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

        // GET: Budget
        public async Task<IActionResult> Index()
        {
            int tenantId = GetCurrentTenantId();

            var budgets = await _context.Budgets
                .Where(b => b.TenantId == tenantId && !b.IsDeleted)
                .Include(b => b.Category)
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Select(b => new BudgetViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Year = b.Year,
                    Month = b.Month,
                    CategoryName = b.Category != null ? b.Category.Name : null,
                    CategoryColor = b.Category != null ? b.Category.Color : null,
                    PlannedIncome = b.PlannedIncome,
                    PlannedExpense = b.PlannedExpense,
                    Notes = b.Notes
                })
                .ToListAsync();

            return View(budgets);
        }

        // GET: Budget/Create
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

            var model = new Budget
            {
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                Name = $"{new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1):MMMM} {DateTime.Now.Year} Budget"
            };

            return View(model);
        }

        // POST: Budget/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Budget budget)
        {
            int tenantId = GetCurrentTenantId();

            if (string.IsNullOrWhiteSpace(budget.Name))
            {
                ModelState.AddModelError("Name", "Budget name is required.");
            }

            if (budget.Year < 2000 || budget.Year > 2100)
            {
                ModelState.AddModelError("Year", "Invalid year.");
            }

            if (budget.Month < 1 || budget.Month > 12)
            {
                ModelState.AddModelError("Month", "Invalid month.");
            }

            budget.TenantId = tenantId;
            budget.CreatedAt = DateTime.UtcNow;
            budget.CreatedBy = User.Identity?.Name ?? "System";

            //if (ModelState.IsValid)
            //{
                try
                {
                    _context.Add(budget);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Budget successfully created!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the budget.");
                }
            //}

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                budget.CategoryId
            );

            return View(budget);
        }

        // GET: Budget/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetCurrentTenantId();

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsDeleted);

            if (budget == null) return NotFound();

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                budget.CategoryId
            );

            return View(budget);
        }

        // POST: Budget/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Budget budget)
        {
            if (id != budget.Id) return NotFound();

            int tenantId = GetCurrentTenantId();

            if (string.IsNullOrWhiteSpace(budget.Name))
            {
                ModelState.AddModelError("Name", "Budget name is required.");
            }

            if (budget.Year < 2000 || budget.Year > 2100)
            {
                ModelState.AddModelError("Year", "Invalid year.");
            }

            if (budget.Month < 1 || budget.Month > 12)
            {
                ModelState.AddModelError("Month", "Invalid month.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Budgets
                        .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsDeleted);

                    if (existing == null) return NotFound();

                    existing.Name = budget.Name;
                    existing.Year = budget.Year;
                    existing.Month = budget.Month;
                    existing.CategoryId = budget.CategoryId;
                    existing.PlannedIncome = budget.PlannedIncome;
                    existing.PlannedExpense = budget.PlannedExpense;
                    existing.Notes = budget.Notes;

                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Budget successfully updated!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while updating the budget.");
                }
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                budget.CategoryId
            );

            return View(budget);
        }

        // POST: Budget/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetCurrentTenantId();

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId && !b.IsDeleted);

            if (budget == null) return NotFound();

            budget.IsDeleted = true;
            budget.UpdatedAt = DateTime.UtcNow; // DeletedAt yo'q, UpdatedAt ishlatamiz
            budget.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
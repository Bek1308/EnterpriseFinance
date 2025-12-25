using EnterpriseFinance.Data;
using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseFinance.Controllers
{
    [Authorize]
    public class PlannedTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlannedTransactionsController(ApplicationDbContext context)
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

        // GET: PlannedTransactions
        public async Task<IActionResult> Index()
        {
            int tenantId = GetCurrentTenantId();

            var planned = await _context.PlannedTransactions
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .Include(p => p.Category)
                .OrderBy(p => p.PlannedDate)
                .Select(p => new PlannedTransactionViewModel
                {
                    Id = p.Id,
                    Name = p.Name, // Yangi qo'shilgan
                    Amount = p.Amount,
                    IsIncome = p.IsIncome,
                    PlannedDate = p.PlannedDate.ToString("yyyy-MM-dd"),
                    ExecutedDate = p.ExecutedDate.HasValue ? p.ExecutedDate.Value.ToString("yyyy-MM-dd"): "Not Executed",
                    CategoryName = p.Category.Name,
                    CategoryColor = p.Category.Color,
                    Description = p.Description,
                    Status = p.Status,
                    IsRecurringGenerated = p.IsRecurringGenerated
                })
                .ToListAsync();

            return View(planned);
        }

        // GET: PlannedTransactions/Create
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

            return View(new PlannedTransaction
            {
                PlannedDate = DateTime.Today,
                Status = "Pending"
            });
        }

        // POST: PlannedTransactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlannedTransaction planned)
        {
            int tenantId = GetCurrentTenantId();

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(planned.Name))
            {
                ModelState.AddModelError("Name", "Transaction name is required.");
                hasError = true;
            }

            if (planned.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
                hasError = true;
            }

            if (planned.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(planned.Status))
            {
                ModelState.AddModelError("Status", "Please select a status.");
                hasError = true;
            }

            planned.TenantId = tenantId;
            planned.CreatedAt = DateTime.UtcNow;
            planned.CreatedBy = User.Identity?.Name ?? "System";

            if (!hasError)
            {
                try
                {
                    _context.Add(planned);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the planned transaction.");
                }
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                planned.CategoryId
            );

            return View(planned);
        }

        // GET: PlannedTransactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetCurrentTenantId();

            var planned = await _context.PlannedTransactions
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted);

            if (planned == null) return NotFound();

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                planned.CategoryId
            );

            return View(planned);
        }

        // POST: PlannedTransactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlannedTransaction planned)
        {
            if (id != planned.Id)
            {
                return NotFound();
            }

            int tenantId = GetCurrentTenantId();

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(planned.Name))
            {
                ModelState.AddModelError("Name", "Transaction name is required.");
                hasError = true;
            }

            if (planned.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
                hasError = true;
            }

            if (planned.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(planned.Status))
            {
                ModelState.AddModelError("Status", "Please select a status.");
                hasError = true;
            }

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
                    planned.CategoryId
                );

                return View(planned);
            }

            try
            {
                var existing = await _context.PlannedTransactions
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted);

                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = planned.Name; // Yangi qo'shilgan
                existing.Amount = planned.Amount;
                existing.IsIncome = planned.IsIncome;
                existing.PlannedDate = planned.PlannedDate;
                existing.ExecutedDate = planned.ExecutedDate;
                existing.CategoryId = planned.CategoryId;
                existing.Description = planned.Description;
                existing.Status = planned.Status;
                existing.IsRecurringGenerated = planned.IsRecurringGenerated;

                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.PlannedTransactions.AnyAsync(p => p.Id == id && p.TenantId == tenantId))
                {
                    return NotFound();
                }

                ModelState.AddModelError("", "The record was modified by another user. Please reload and try again.");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the planned transaction. Please try again.");
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                planned.CategoryId
            );

            return View(planned);
        }

        // POST: PlannedTransactions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetCurrentTenantId();

            var planned = await _context.PlannedTransactions
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted);

            if (planned == null) return NotFound();

            planned.IsDeleted = true;
            planned.DeletedAt = DateTime.UtcNow;
            planned.DeletedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
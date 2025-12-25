using EnterpriseFinance.Data;
using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseFinance.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
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

        // GET: Transactions
        public async Task<IActionResult> Index()
        {
            int tenantId = GetCurrentTenantId();

            var transactions = await _context.Transactions
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Amount,
                    Date = t.Date.ToString("yyyy-MM-dd"),
                    t.Description,
                    CategoryName = t.Category.Name,
                    CategoryColor = t.Category.Color,
                    IsIncome = t.Category.IsIncome
                })
                .ToListAsync();

            return View(transactions.Cast<object>().ToList());
        }

        // GET: Transactions/Create
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

            return View(new Transaction { Date = DateTime.Today });
        }

        // POST: Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            int tenantId = GetCurrentTenantId();

            if (string.IsNullOrWhiteSpace(transaction.Name))
            {
                ModelState.AddModelError("Name", "Transaction name is required.");
            }

            if (transaction.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
            }

            if (transaction.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
            }

            transaction.TenantId = tenantId;
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.CreatedBy = User.Identity?.Name ?? "System";

            //if (ModelState.IsValid)
            //{
                try
                {
                    _context.Add(transaction);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the transaction.");
                }
            //}

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                transaction.CategoryId
            );

            return View(transaction);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetCurrentTenantId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && !t.IsDeleted);

            if (transaction == null) return NotFound();

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                transaction.CategoryId
            );

            return View(transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            if (id != transaction.Id) return NotFound();

            int tenantId = GetCurrentTenantId();

            if (string.IsNullOrWhiteSpace(transaction.Name))
            {
                ModelState.AddModelError("Name", "Transaction name is required.");
            }

            if (transaction.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category.");
            }

            if (transaction.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    var existing = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && !t.IsDeleted);

                    if (existing == null) return NotFound();

                    existing.Name = transaction.Name;
                    existing.Amount = transaction.Amount;
                    existing.Date = transaction.Date;
                    existing.CategoryId = transaction.CategoryId;
                    existing.Description = transaction.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while updating the transaction.");
                }
            //}

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id",
                "Name",
                transaction.CategoryId
            );

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetCurrentTenantId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && !t.IsDeleted);

            if (transaction == null) return NotFound();

            transaction.IsDeleted = true;
            transaction.DeletedAt = DateTime.UtcNow;
            transaction.DeletedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
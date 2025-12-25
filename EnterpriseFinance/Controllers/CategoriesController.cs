using EnterpriseFinance.Data;
using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFinance.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
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

        // GET: Categories
        [Authorize]
        public async Task<IActionResult> Index(string search, bool? isIncome)
        {
            int tenantId = GetCurrentTenantId();

            // Asosiy query – IQueryable
            IQueryable<Category> categories = _context.Categories
                .Where(c => c.TenantId == tenantId && !c.IsDeleted);

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                categories = categories.Where(c => c.Name.Contains(search));
            }

            // IsIncome filter
            if (isIncome.HasValue)
            {
                categories = categories.Where(c => c.IsIncome == isIncome.Value);
            }

            // OrderBy ni alohida o‘zgaruvchiga olib, tipni IOrderedQueryable qilamiz
            var orderedCategories = categories
                .OrderBy(c => c.OrderIndex ?? 999999) // NULL qiymatlar oxirga
                .ThenBy(c => c.Name);

            ViewData["Search"] = search;
            ViewData["IsIncome"] = isIncome;

            var list = await orderedCategories.ToListAsync();
            return View(list);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            int tenantId = GetCurrentTenantId();

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Kategoriya nomi majburiy.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(category.Color))
            {
                category.Color = "#6b7280";
            }

            category.TenantId = tenantId;
            category.CreatedAt = DateTime.UtcNow;
            category.CreatedBy = User.Identity?.Name ?? "System";

            if (isValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetCurrentTenantId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            int tenantId = GetCurrentTenantId();

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Kategoriya nomi majburiy.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(category.Color))
            {
                category.Color = "#6b7280";
            }

            if (isValid)
            {
                var existing = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);

                if (existing == null) return NotFound();

                existing.Name = category.Name;
                existing.IsIncome = category.IsIncome;
                existing.Color = category.Color;
                existing.OrderIndex = category.OrderIndex;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetCurrentTenantId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted);

            if (category == null) return NotFound();

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
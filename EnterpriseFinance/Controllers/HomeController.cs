using EnterpriseFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseFinance.Controllers
{
    [Authorize] // <-- Muhim: Faqat kirgan foydalanuvchilar ko‘ra oladi
    public class HomeController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        public HomeController(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous] // <-- Istisno – kirishsiz ham ko‘rish mumkin (agar kerak bo‘lsa)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage("");
            }
        }
    }
}
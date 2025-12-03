using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BasketWorld.Controllers
{
    [Authorize]
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopController(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "Session expirée, veuillez vous reconnecter.";
                return RedirectToAction("Login", "Account");
            }

            // Simuler un achat
            user.Coins += amount;
            await _ctx.SaveChangesAsync();

            TempData["ok"] = $"✅ Vous avez acheté {amount} coins !";
            return RedirectToAction(nameof(Index));
        }
    }
}

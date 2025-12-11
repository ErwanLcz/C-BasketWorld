using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BasketWorld.Controllers
{
    [Authorize]
    [Route("Checkout")]
    public class CheckoutController : Controller
    {
        private static readonly int[] AllowedPacks = new[] { 10, 50, 100 };

        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        [HttpGet("Start")]
        public IActionResult Start(int amount)
        {
            if (!AllowedPacks.Contains(amount))
            {
                TempData["err"] = "Montant invalide.";
                return RedirectToAction("Index", "Shop");
            }

            var vm = new FakeCheckoutViewModel
            {
                Amount = amount,
                ProductLabel = $"{amount} coins"
            };
            return View("Index", vm);
        }

        [HttpPost("Pay")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(FakeCheckoutPostModel form)
        {
            // On NE valide PAS réellement la carte : tout passe.
            if (!AllowedPacks.Contains(form.Amount))
            {
                TempData["err"] = "Montant invalide.";
                return RedirectToAction("Index", "Shop");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "Session expirée, veuillez vous reconnecter.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            user.Coins += form.Amount;
            await _ctx.SaveChangesAsync();

            TempData["ok"] = $"Paiement simulé réussi ✅ Vous avez reçu {form.Amount} coins.";
            return RedirectToAction("Index", "Shop");
        }
    }

    public class FakeCheckoutViewModel
    {
        public int Amount { get; set; }
        public string ProductLabel { get; set; } = "";
    }

    public class FakeCheckoutPostModel
    {
        public int Amount { get; set; }
        // Champs “faux” juste pour l’UI
        public string Cardholder { get; set; } = "";
        public string CardNumber { get; set; } = "";
        public string ExpMonth { get; set; } = "";
        public string ExpYear { get; set; } = "";
        public string Cvc { get; set; } = "";
    }
}

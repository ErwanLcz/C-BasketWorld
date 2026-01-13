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
        // Prix en € -> Nombre de coins
        private static readonly Dictionary<int, int> PriceToCoinsPacks = new()
        {
            { 10, 10 },   // 10€ = 10 coins
            { 45, 50 },   // 45€ = 50 coins
            { 90, 100 }   // 90€ = 100 coins
        };

        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        /// <summary>
        /// Convertit un prix en nombre de coins
        /// </summary>
        private int GetCoinsForPrice(int price)
        {
            return PriceToCoinsPacks.TryGetValue(price, out var coins) ? coins : 0;
        }

        [HttpGet("Start")]
        public IActionResult Start(int amount)
        {
            if (!PriceToCoinsPacks.ContainsKey(amount))
            {
                TempData["err"] = "Montant invalide.";
                return RedirectToAction("Index", "Shop");
            }

            var coins = GetCoinsForPrice(amount);
            var vm = new FakeCheckoutViewModel
            {
                Amount = amount,
                Coins = coins,
                ProductLabel = $"{coins} crédits"
            };
            return View("Index", vm);
        }

        [HttpPost("Pay")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(FakeCheckoutPostModel form)
        {
            // On NE valide PAS réellement la carte : tout passe.
            if (!PriceToCoinsPacks.ContainsKey(form.Amount))
            {
                TempData["toast_type"] = "error";
                TempData["toast_title"] = "Oups…";
                TempData["toast_msg"] = "Montant invalide.";
                return RedirectToAction("Index", "Shop");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "Session expirée, veuillez vous reconnecter.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var coins = GetCoinsForPrice(form.Amount);
            user.crédits += coins;
            await _ctx.SaveChangesAsync();

            TempData["toast_type"] = "success";          // success | error | info
            TempData["toast_title"] = "Paiement validé ✅";
            TempData["toast_msg"] = $"Tu as reçu +{coins} coins. Bon match !";
            return RedirectToAction("Index", "Shop");
        }
    }

    public class FakeCheckoutViewModel
    {
        public int Amount { get; set; }      // Prix en €
        public int Coins { get; set; }       // Nombre de coins reçus
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

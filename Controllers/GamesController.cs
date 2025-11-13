using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace BasketWorld.Controllers
{
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public GamesController(ApplicationDbContext ctx) { _ctx = ctx; }
        private static readonly TimeSpan EarlyAccessWindow = TimeSpan.FromMinutes(2); // optionnel


        public async Task<IActionResult> Details(int id)
        {
            var game = await _ctx.Games
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.League)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return NotFound();

            var hasAccess = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                hasAccess = await _ctx.GameAccesses.AnyAsync(a => a.GameId == id && a.UserId == userId);
            }

            ViewBag.HasAccess = hasAccess;
            ViewBag.CanWatchFromUtc = game.StartAt - EarlyAccessWindow;
            return View(game);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyAccess(int id)
        {
            var game = await _ctx.Games.FindAsync(id);
            if (game == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _ctx.Users.FirstAsync(u => u.Id == userId);

            // déjà acheté ?
            var already = await _ctx.GameAccesses.AnyAsync(a => a.GameId == id && a.UserId == userId);
            if (already)
            {
                TempData["ok"] = "Vous avez déjà l’accès à ce match.";
                return RedirectToAction(nameof(Watch), new { id });
            }

            // assez de coins ?
            if (user.Coins < 1)
            {
                TempData["err"] = "Solde insuffisant (1 coin nécessaire).";
                return RedirectToAction(nameof(Details), new { id });
            }

            user.Coins -= 1;
            _ctx.GameAccesses.Add(new GameAccess { GameId = id, UserId = userId });
            await _ctx.SaveChangesAsync();

            TempData["ok"] = "Accès accordé ! Bon match.";
            return RedirectToAction(nameof(Watch), new { id });
        }

        [Authorize]
        public async Task<IActionResult> Watch(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var access = await _ctx.GameAccesses
                .Include(a => a.Game)
                    .ThenInclude(g => g.HomeTeam)
                .Include(a => a.Game)
                    .ThenInclude(g => g.AwayTeam)
                .FirstOrDefaultAsync(a => a.GameId == id && a.UserId == userId);

            if (access == null)
            {
                TempData["err"] = "Vous n’avez pas réservé ce match.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var game = access.Game;
            var nowUtc = DateTime.UtcNow;

            // Autoriser à partir de StartAt (avec fenêtre optionnelle)
            var watchFromUtc = game.StartAt - EarlyAccessWindow;

            if (nowUtc < watchFromUtc)
            {
                var localStart = game.StartAt.ToLocalTime();
                TempData["err"] = $"La diffusion sera disponible le {localStart:dd/MM/yyyy HH:mm}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View("Watch", game);
        }
        // GamesController.cs
        [Authorize]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!; // <= au lieu de _userManager.GetUserId(User)
            var games = await _ctx.GameAccesses
                .Where(a => a.UserId == userId)
                .Include(a => a.Game).ThenInclude(g => g.HomeTeam)
                .Include(a => a.Game).ThenInclude(g => g.AwayTeam)
                .Include(a => a.Game).ThenInclude(g => g.League)
                .Select(a => a.Game)
                .AsNoTracking()
                .ToListAsync();

            var now = DateTime.UtcNow;
            ViewBag.Upcoming = games.Where(g => g.StartAt > now).OrderBy(g => g.StartAt).ToList();
            ViewBag.Past     = games.Where(g => g.StartAt <= now).OrderByDescending(g => g.StartAt).ToList();

            return View("Mine");
        }
    }
}

using BasketWorld.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BasketWorld.Data;

namespace BasketWorld.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class DashboardController : Controller
    {
        private readonly NbaSyncService _sync;
        private readonly ApplicationDbContext _ctx;

        public DashboardController(NbaSyncService sync, ApplicationDbContext ctx)
        {
            _sync = sync;
            _ctx = ctx;
        }

        /// <summary>
        /// Lance une synchronisation NBA.
        /// mode = "exact" (par défaut) : une seule fenêtre past/next
        /// mode = "expand" : essaie plusieurs fenêtres successives (pour obtenir des matchs "Final")
        /// </summary>
        public async Task<IActionResult> SyncNba(int past = 2, int next = 5, string mode = "exact", int minUpserts = 1)
        {
            try
            {
                var totalTeams = 0;
                var totalGames = 0;

                // Définit les fenêtres à essayer
                var windows = new List<(int past, int next, string label)>();
                if (string.Equals(mode, "expand", StringComparison.OrdinalIgnoreCase))
                {
                    windows.Add((past, next, $"({past}j / +{next}j)"));
                    windows.Add((30, 7, "(30j / +7j)"));
                    windows.Add((120, 0, "(120j passés)"));
                    windows.Add((365, 0, "(365j passés)"));
                }
                else
                {
                    windows.Add((past, next, $"({past}j / +{next}j)"));
                }

                string used = "";

                foreach (var w in windows)
                {
                    var from = DateTime.UtcNow.Date.AddDays(-w.past);
                    var to   = DateTime.UtcNow.Date.AddDays(+w.next);

                    var (t, g) = await _sync.SyncAsync(from, to);
                    totalTeams += t;
                    totalGames += g;
                    used += (used.Length == 0 ? "" : " → ") + w.label;

                    if (string.Equals(mode, "expand", StringComparison.OrdinalIgnoreCase)
                        && (t + g) >= minUpserts)
                    {
                        break;
                    }

                    await Task.Delay(200); // petite pause pour la rate limit
                }

                TempData["msg"] = $"Sync OK. Teams upserts: {totalTeams}, Games upserts: {totalGames}. Fenêtres: {used}.";
            }
            catch (Exception ex)
            {
                TempData["err"] = "Échec sync NBA : " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var nba = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == "NBA");
            var games = Enumerable.Empty<Models.Game>();
            if (nba != null)
            {
                games = await _ctx.Games
                    .Where(g => g.LeagueId == nba.Id && g.Source == "balldontlie")
                    .Include(g => g.HomeTeam)
                    .Include(g => g.AwayTeam)
                    .OrderByDescending(g => g.StartAt)
                    .Take(50)
                    .ToListAsync();
            }
            return View(games);
        }
    }
}

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

        // mode = "exact" (par défaut) => tente uniquement la fenêtre past/next
        // mode = "expand" => essaie plusieurs fenêtres successives et additionne les upserts
        public async Task<IActionResult> SyncNba(int past = 2, int next = 5, string mode = "exact", int minMatches = 1)
        {
            try
            {
                var attempts = new List<(int past, int next, string label)>();

                if (string.Equals(mode, "expand", StringComparison.OrdinalIgnoreCase))
                {
                    attempts.Add((past, next, $"({past}j / +{next}j)"));
                    attempts.Add((30, 7, "(30j / +7j)"));
                    attempts.Add((120, 0, "(120j passés)"));
                    attempts.Add((365, 0, "(365j passés)"));
                }
                else
                {
                    attempts.Add((past, next, $"({past}j / +{next}j)"));
                }

                var totalUpserts = 0;
                string usedWindows = "";

                foreach (var a in attempts)
                {
                    var from = DateTime.UtcNow.Date.AddDays(-a.past);
                    var to   = DateTime.UtcNow.Date.AddDays(+a.next);

                    var count = await _sync.SyncAsync(from, to);
                    totalUpserts += count;

                    usedWindows += (usedWindows.Length == 0 ? "" : " → ") + a.label;

                    // si mode=expand et on veut juste garantir ≥ minMatches,
                    // on peut s'arrêter dès qu'on a atteint le quota
                    if (string.Equals(mode, "expand", StringComparison.OrdinalIgnoreCase) && totalUpserts >= minMatches)
                        break;

                    await Task.Delay(250); // respect du rate limit
                }

                TempData["msg"] = $"Sync NBA OK — upserts: {totalUpserts}. Fenêtres: {usedWindows}.";
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

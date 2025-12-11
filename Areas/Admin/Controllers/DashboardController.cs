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
        private readonly EuroleagueSyncService _euroSync;
        private readonly EuroleagueOfficialSyncService _euroOfficial;


        public DashboardController(
            NbaSyncService sync,
            EuroleagueSyncService euroSync,
            EuroleagueOfficialSyncService euroOfficial,
            ApplicationDbContext ctx)
        {
            _sync = sync;
            _euroSync = euroSync;
            _euroOfficial = euroOfficial;  // ✅ maintenant c'est un paramètre
            _ctx = ctx;
        }


        /// <summary>
        /// Lance une synchronisation NBA.
        /// mode = "exact" (par défaut) : une seule fenêtre past/next
        /// mode = "expand" : essaie plusieurs fenêtres successives (pour obtenir des matchs "Final")
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncEuroleagueOfficial(string seasonCode = "E2025")
        {
            var upserts = await _euroOfficial.SyncSeasonAsync(seasonCode);
            TempData["msg"] = $"EuroLeague (officielle) : {upserts} matchs synchronisés pour {seasonCode}.";
            return RedirectToAction("Index");
        }
        
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncEuroleagueTeams()
        {
            try
            {
                var upserts = await _euroSync.SyncTeamsAsync();
                TempData["msg"] = $"EuroLeague : {upserts} équipes synchronisées.";
            }
            catch (Exception ex)
            {
                TempData["err"] = "Erreur sync EuroLeague teams : " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncEuroleagueSeason(string season = "2025")
        {
            try
            {
                var upserts = await _euroSync.SyncSeasonGamesAsync(season);
                TempData["msg"] = $"EuroLeague : {upserts} matchs synchronisés pour {season}.";
            }
            catch (Exception ex)
            {
                TempData["err"] = "Erreur sync EuroLeague games : " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        


    }
}

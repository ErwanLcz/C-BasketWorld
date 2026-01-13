using BasketWorld.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BasketWorld.Data;
using Microsoft.AspNetCore.Identity;
using BasketWorld.Models;

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
        private readonly UserManager<ApplicationUser> _userManager;


        public DashboardController(
            NbaSyncService sync,
            EuroleagueSyncService euroSync,
            EuroleagueOfficialSyncService euroOfficial,
            ApplicationDbContext ctx,
            UserManager<ApplicationUser> userManager)
        {
            _sync = sync;
            _euroSync = euroSync;
            _euroOfficial = euroOfficial;
            _ctx = ctx;
            _userManager = userManager;
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

        // Areas/Admin/Controllers/DashboardController.cs
        public async Task<IActionResult> SyncNbaSeason(int season = 2025)
        {
            try
            {
                // Saison NBA 2025 = Oct 2025 -> Juin 2026 (tu peux élargir si tu veux playoffs)
                var from = new DateTime(season, 10, 1, 0, 0, 0, DateTimeKind.Utc);
                var to   = new DateTime(season + 1, 6, 30, 0, 0, 0, DateTimeKind.Utc);

                var (t, g) = await _sync.SyncAsync(from, to);
                TempData["msg"] = $"Sync NBA saison {season}: Teams upserts={t}, Games upserts={g}.";
            }
            catch (Exception ex)
            {
                TempData["err"] = "Échec sync NBA saison : " + ex.Message;
            }

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Index()
        {
            var users = await _ctx.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync();
            
            return View(users);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(string userId, string newRole, int creditAmount)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["err"] = "Utilisateur non trouvé.";
                    return RedirectToAction("Index");
                }

                // Mettre à jour le rôle
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                if (!string.IsNullOrEmpty(newRole))
                {
                    await _userManager.AddToRoleAsync(user, newRole);
                }

                // Mettre à jour les crédits
                user.crédits = creditAmount;
                _ctx.Users.Update(user);
                await _ctx.SaveChangesAsync();

                TempData["msg"] = $"Utilisateur {user.UserName} mis à jour avec succès.";
            }
            catch (Exception ex)
            {
                TempData["err"] = "Erreur mise à jour : " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

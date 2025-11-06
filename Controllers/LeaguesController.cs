using BasketWorld.Data;
using Microsoft.AspNetCore.Mvc;
using BasketWorld.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Controllers
{
    public class LeaguesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public LeaguesController(ApplicationDbContext ctx) { _ctx = ctx; }

        [HttpGet("/leagues/{name}")]
        public async Task<IActionResult> Details(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();

            var league = await _ctx.Leagues
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.Name == name);
            if (league == null) return NotFound();

            var nowUtc = DateTime.UtcNow;

            var games = await _ctx.Games
                .AsNoTracking()
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Where(g => g.LeagueId == league.Id)
                .OrderBy(g => g.StartAt)
                .ToListAsync();

            // --- NE PRENDRE QUE LES ÉQUIPES QUI APPARAISSENT DANS LES MATCHS ---
            var teamIds = games.Select(g => g.HomeTeamId)
                            .Concat(games.Select(g => g.AwayTeamId))
                            .Distinct()
                            .ToHashSet();

            var teamsThatActuallyPlay = league.Teams
                .Where(t => teamIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ToList();

            var vm = new LeaguePageViewModel
            {
                LeagueName = league.Name,
                Teams = teamsThatActuallyPlay,
                UpcomingGames = games.Where(g => g.StartAt >= nowUtc)
                                    .OrderBy(g => g.StartAt)
                                    .ToList(),
                PastGames = games.Where(g => g.StartAt < nowUtc)
                                .OrderByDescending(g => g.StartAt)
                                .Take(30)
                                .ToList()
            };

            return View("Details", vm);
        }
    }
}

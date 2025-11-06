using BasketWorld.Data;
using Microsoft.AspNetCore.Mvc;
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
            var league = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == name);
            if (league == null) return NotFound();

            var games = await _ctx.Games
                .Where(g => g.LeagueId == league.Id)
                .Include(g => g.HomeTeam).Include(g => g.AwayTeam)
                .OrderBy(g => g.StartAt).ToListAsync();

            ViewBag.LeagueName = league.Name;
            return View(games); // Views/Leagues/Details.cshtml
        }
    }
}

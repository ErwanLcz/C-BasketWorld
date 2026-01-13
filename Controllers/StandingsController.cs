using BasketWorld.Data;
using BasketWorld.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Controllers
{
    public class StandingsController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly StandingsService _svc;

        public StandingsController(ApplicationDbContext ctx, StandingsService svc)
        {
            _ctx = ctx;
            _svc = svc;
        }

        [HttpGet("/standings/{leagueName?}")]
        public async Task<IActionResult> Index(string? leagueName, int? season)
        {
            leagueName ??= "NBA";

            var league = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == leagueName);
            if (league == null) return NotFound("League not found");

            // Convertir l'année en saison (ex: 2026 -> saison 2025-2026)
            int seasonStartYear = season ?? (DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1);
            string seasonDisplay = $"{seasonStartYear}-{seasonStartYear + 1}";

            var standings = await _svc.GetStandingsAsync(league.Id, seasonStartYear);

            ViewBag.League = league;
            ViewBag.Season = seasonDisplay;
            return View(standings);
        }
    }
}

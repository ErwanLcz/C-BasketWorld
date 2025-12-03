using BasketWorld.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public HomeController(ApplicationDbContext ctx) { _ctx = ctx; }

        public async Task<IActionResult> Index()
        {
            // Borne: début de "aujourd'hui" en heure locale → converti en UTC (si StartAt est en UTC)
            var todayLocal = DateTime.Now.Date; // 00:00 locale
            var todayUtc = DateTime.SpecifyKind(todayLocal, DateTimeKind.Local).ToUniversalTime();

            var last3 = await _ctx.Games
                .Where(g => g.StartAt < todayUtc) // strictement avant aujourd'hui
                .Include(g => g.HomeTeam).Include(g => g.AwayTeam).Include(g => g.League)
                .OrderByDescending(g => g.StartAt)
                .Take(3)
                .ToListAsync();

            var next3 = await _ctx.Games
                .Where(g => g.StartAt >= todayUtc) // aujourd'hui (inclus) + futur
                .Include(g => g.HomeTeam).Include(g => g.AwayTeam).Include(g => g.League)
                .OrderBy(g => g.StartAt)
                .Take(3)
                .ToListAsync();

            ViewBag.Last3 = last3;
            ViewBag.Next3 = next3;
            return View();
        }

    }
}

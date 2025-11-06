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
            var now = DateTime.UtcNow;

            var last3 = await _ctx.Games
                .Include(g => g.League).Include(g => g.HomeTeam).Include(g => g.AwayTeam)
                .Where(g => g.StartAt < now).OrderByDescending(g => g.StartAt).Take(3).ToListAsync();

            var next3 = await _ctx.Games
                .Include(g => g.League).Include(g => g.HomeTeam).Include(g => g.AwayTeam)
                .Where(g => g.StartAt >= now).OrderBy(g => g.StartAt).Take(3).ToListAsync();

            ViewBag.Last3 = last3;
            ViewBag.Next3 = next3;
            return View();
        }
    }
}

using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public TeamsController(ApplicationDbContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Leagues = await _ctx.Leagues.OrderBy(l => l.Name).ToListAsync();
            return View(new Team());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Leagues = await _ctx.Leagues.OrderBy(l => l.Name).ToListAsync();
                return View(team);
            }
            _ctx.Teams.Add(team);
            await _ctx.SaveChangesAsync();
            TempData["ok"] = "Équipe ajoutée.";
            return RedirectToAction(nameof(Create));
        }
    }
}

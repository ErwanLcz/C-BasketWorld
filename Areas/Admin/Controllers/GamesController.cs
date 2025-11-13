using BasketWorld.Data;
using BasketWorld.Models;
using BasketWorld.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public GamesController(ApplicationDbContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillDropdowns();
            return View(new GameCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                await FillDropdowns();
                return View(vm);
            }

            if (vm.HomeTeamId == vm.AwayTeamId)
            {
                ModelState.AddModelError("", "L'équipe domicile et l'équipe extérieure doivent être différentes.");
                await FillDropdowns();
                return View(vm);
            }

            var game = new Game
            {
                LeagueId = vm.LeagueId,
                HomeTeamId = vm.HomeTeamId,
                AwayTeamId = vm.AwayTeamId,
                StartAt = vm.StartAt,
                Venue = vm.Venue
            };

            _ctx.Games.Add(game);
            await _ctx.SaveChangesAsync();
            TempData["ok"] = "Match créé avec succès.";
            return RedirectToAction("Create");
        }

        private async Task FillDropdowns()
        {
            ViewBag.Leagues = await _ctx.Leagues.OrderBy(l => l.Name).ToListAsync();
            ViewBag.SeatCategories = await _ctx.SeatCategories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Teams = await _ctx.Teams.OrderBy(t => t.Name).ToListAsync();
        }
    }
}
